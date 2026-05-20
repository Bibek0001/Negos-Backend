using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Startup guard — fail fast with a clear message if secrets are placeholders.
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("REPLACE_") || jwtKey.StartsWith("CHANGE_"))
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Set a strong secret (≥32 chars) via the " +
        "Jwt__Key environment variable or appsettings.Production.json.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("YOUR_PRODUCTION"))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. " +
        "Set it via the ConnectionStrings__DefaultConnection environment variable.");

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();          // needed by TenantService
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<EmailService>();

// Database — auto-detects provider from the connection string:
//   • Starts with "Host=", "postgresql", or "postgres" → PostgreSQL (Npgsql)
//   • Starts with "Data Source=" or ends with ".db" / ".sqlite" → SQLite
//   • Anything else → SQL Server (default, works with SQLEXPRESS and full SQL Server)
var usePostgres = connStr.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)
               || connStr.StartsWith("postgresql", StringComparison.OrdinalIgnoreCase)
               || connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase);

var useSqlite   = !usePostgres && (
                    connStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                 || connStr.EndsWith(".db",     StringComparison.OrdinalIgnoreCase)
                 || connStr.EndsWith(".sqlite",  StringComparison.OrdinalIgnoreCase));

// Convert postgres:// URL format to Npgsql connection string format
// Uses regex to safely handle passwords with special characters like @
if (usePostgres && (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://")))
{
    try
    {
        // Pattern: postgres://username:password@host:port/database
        // The password may contain @ so we match from the last @ before host
        var withoutScheme = System.Text.RegularExpressions.Regex.Replace(connStr, @"^postgres(ql)?://", "");
        // Find last @ to split userinfo from host
        var lastAt = withoutScheme.LastIndexOf('@');
        var userInfo = withoutScheme.Substring(0, lastAt);
        var hostPart = withoutScheme.Substring(lastAt + 1);
        var colonInUser = userInfo.IndexOf(':');
        var username = colonInUser >= 0 ? userInfo.Substring(0, colonInUser) : userInfo;
        var password = colonInUser >= 0 ? userInfo.Substring(colonInUser + 1) : "";
        // Parse host:port/database
        var slashIdx = hostPart.IndexOf('/');
        var hostPort = slashIdx >= 0 ? hostPart.Substring(0, slashIdx) : hostPart;
        var database = slashIdx >= 0 ? hostPart.Substring(slashIdx + 1).Split('?')[0] : "postgres";
        var colonInHost = hostPort.LastIndexOf(':');
        var host = colonInHost >= 0 ? hostPort.Substring(0, colonInHost) : hostPort;
        var port = colonInHost >= 0 ? hostPort.Substring(colonInHost + 1) : "5432";
        // Decode URL-encoded characters
        username = Uri.UnescapeDataString(username);
        password = Uri.UnescapeDataString(password);
        connStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not parse PostgreSQL URL: {ex.Message}. Using as-is.");
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(connStr, npgsql =>
        {
            npgsql.EnableRetryOnFailure(3);
        });
    }
    else if (useSqlite) options.UseSqlite(connStr);
    else                options.UseSqlServer(connStr);
});

// ---------------------------------------------------------------------------
// JWT Authentication
// Token carries: Name (username), TenantId, Role
// ---------------------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer            = true,
            ValidateAudience          = true,
            ValidateLifetime          = true,
            ValidateIssuerSigningKey  = true,
            ValidIssuer               = builder.Configuration["Jwt:Issuer"],
            ValidAudience             = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey          = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin"));
    options.AddPolicy("AnyAdmin",   p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin", "Admin"));
});

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
    {
        var originsConfig = builder.Configuration["AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(originsConfig) && originsConfig != "*")
        {
            var origins = originsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries);
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // Allow all origins (dev / initial deploy)
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    }));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Startup: migrate + seed (non-fatal — app starts even if DB is unavailable)
// ---------------------------------------------------------------------------
try
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var migrationOptions = new DbContextOptionsBuilder<AppDbContext>();
        if (usePostgres) migrationOptions.UseNpgsql(connStr);
        else if (useSqlite) migrationOptions.UseSqlite(connStr);
        else migrationOptions.UseSqlServer(connStr);
        using var migrationDb = new AppDbContext(migrationOptions.Options);
        logger.LogInformation("Starting database migration...");
        migrationDb.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed. Verify connection string.");
        throw;
    }
    logger.LogInformation("Migration completed successfully.");

    // -----------------------------------------------------------------------
    // Seed default tenants (3 tenants for the platform demo)
    // -----------------------------------------------------------------------
    var tenantsToSeed = new[]
    {
        new { Id = 1, Subdomain = "negos",           Name = "Negos",               Email = "contact@negos.org",           AdminUser = "admin_diyalo",   AdminPass = "Diyalo@123"    },
        new { Id = 2, Subdomain = "volunteernepal",  Name = "Volunteer Nepal",      Email = "contact@volunteernepal.org",  AdminUser = "admin_volunteer",        AdminPass = "Volunteer@123" },
        new { Id = 3, Subdomain = "nepalhelp",       Name = "Nepal Help Foundation",Email = "contact@nepalhelp.org",       AdminUser = "admin_nepalhelp",        AdminPass = "NepalHelp@123" },
    };

    foreach (var t in tenantsToSeed)
    {
        // Ensure tenant exists
        var tenant = db.Tenants.FirstOrDefault(x => x.Id == t.Id)
                  ?? db.Tenants.FirstOrDefault(x => x.Subdomain == t.Subdomain);

        if (tenant == null)
        {
            tenant = new Tenant
            {
                Subdomain = t.Subdomain,
                Name      = t.Name,
                Email     = t.Email,
                IsActive  = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Tenants.Add(tenant);
            db.SaveChanges();

            // Provision default content for this tenant
            var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
            await provisioning.ProvisionAsync(tenant);
        }
        else
        {
            // Ensure existing tenant has content
            var hasData = db.MenuItems.Any(m => m.TenantId == tenant.Id);
            if (!hasData)
            {
                var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
                await provisioning.ProvisionAsync(tenant);
            }
            else
            {
                // Tenant has menus but may be missing programs/news/tours/testimonials
                var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
                await provisioning.SeedContentIfMissingAsync(tenant);
            }
        }

        // Ensure tenant admin account exists with correct password
        var adminHash = BCrypt.Net.BCrypt.HashPassword(t.AdminPass);
        var admin = db.AdminUsers.FirstOrDefault(u => u.Username == t.AdminUser && u.TenantId == tenant.Id);
        if (admin == null)
            db.AdminUsers.Add(new AdminUser { Username = t.AdminUser, PasswordHash = adminHash, TenantId = tenant.Id, Role = "Admin" });
        else
        { admin.PasswordHash = adminHash; admin.Role = "Admin"; admin.TenantId = tenant.Id; }
    }

    db.SaveChanges();

    // Remove any stale legacy admin accounts from old branding
    var staleAdmins = db.AdminUsers.Where(u => u.Username == "admin_negos").ToList();
    if (staleAdmins.Any()) { db.AdminUsers.RemoveRange(staleAdmins); db.SaveChanges(); }

    // -----------------------------------------------------------------------
    // ALL Admin accounts — FIXED credentials, reset on every startup.
    // SuperAdmin : Negos      / Negos@123     (TenantId=0)
    // SuperAdmin : NegosBk    / NegosBk@2026  (TenantId=0)
    // Tenant 1   : admin_diyalo   / Diyalo@123    (TenantId=1)
    // Tenant 2   : admin_volunteer / Volunteer@123 (TenantId=2)
    // Tenant 3   : admin_nepalhelp / NepalHelp@123 (TenantId=3)
    // -----------------------------------------------------------------------
    const string primaryHash    = "$2a$11$d2otSTC/OPK.lifVGrU0iuqLja245JkNirM92Vp7l7kbfKN3ArUcy";
    const string backupHash     = "$2a$11$EewzvCHCXARXpWrEFtkVWuew7MyK8bS0qOCma96ZCY7tozIGNc4Dm";

    // SuperAdmin accounts
    var primary = db.AdminUsers.FirstOrDefault(u => u.Username == "Negos");
    if (primary == null)
        db.AdminUsers.Add(new AdminUser { Username = "Negos", PasswordHash = primaryHash, TenantId = 0, Role = "SuperAdmin" });
    else
    { primary.PasswordHash = primaryHash; primary.Role = "SuperAdmin"; primary.TenantId = 0; }

    var backup = db.AdminUsers.FirstOrDefault(u => u.Username == "NegosBk");
    if (backup == null)
        db.AdminUsers.Add(new AdminUser { Username = "NegosBk", PasswordHash = backupHash, TenantId = 0, Role = "SuperAdmin" });
    else
    { backup.PasswordHash = backupHash; backup.Role = "SuperAdmin"; backup.TenantId = 0; }

    db.SaveChanges();

    // Tenant admin accounts — always upsert with correct password
    var tenantAdmins = new[]
    {
        new { Username = "admin_diyalo",    Password = "Diyalo@123",    TenantId = 1 },
        new { Username = "admin_volunteer", Password = "Volunteer@123", TenantId = 2 },
        new { Username = "admin_nepalhelp", Password = "NepalHelp@123", TenantId = 3 },
    };

    foreach (var ta in tenantAdmins)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(ta.Password);
        var existing = db.AdminUsers.FirstOrDefault(u => u.Username == ta.Username);
        if (existing == null)
            db.AdminUsers.Add(new AdminUser { Username = ta.Username, PasswordHash = hash, TenantId = ta.TenantId, Role = "Admin" });
        else
        { existing.PasswordHash = hash; existing.Role = "Admin"; existing.TenantId = ta.TenantId; }
    }

    db.SaveChanges();

    // -----------------------------------------------------------------------
    // Seed platform menu items (TenantId=0) — used by the public landing page
    // and the Super Admin NavbarMenus page.
    // Only seed if none exist yet.
    // -----------------------------------------------------------------------
    if (!db.MenuItems.Any(m => m.TenantId == 0))
    {
        var pHome       = new MenuItem { TenantId = 0, Label = "Home",       Url = "/",                        IsVisible = true, Order = 1  };
        var pAbout      = new MenuItem { TenantId = 0, Label = "About Us",   Url = "/about",                   IsVisible = true, Order = 2  };
        var pPlacements = new MenuItem { TenantId = 0, Label = "Placements", Url = "/placements/volunteering", IsVisible = true, Order = 3  };
        var pImpact     = new MenuItem { TenantId = 0, Label = "Our Impact", Url = "/our-impact",              IsVisible = true, Order = 4  };
        var pLocations  = new MenuItem { TenantId = 0, Label = "Locations",  Url = "/locations",               IsVisible = true, Order = 5  };
        var pFees       = new MenuItem { TenantId = 0, Label = "Fees",       Url = "/fees",                    IsVisible = true, Order = 6  };
        var pFaqs       = new MenuItem { TenantId = 0, Label = "FAQs",       Url = "/faqs",                    IsVisible = true, Order = 7  };
        var pNews       = new MenuItem { TenantId = 0, Label = "News",       Url = "/news",                    IsVisible = true, Order = 8  };
        var pApply      = new MenuItem { TenantId = 0, Label = "Apply Now",  Url = "/apply",                   IsVisible = true, Order = 9  };
        var pContact    = new MenuItem { TenantId = 0, Label = "Contact Us", Url = "/contact",                 IsVisible = true, Order = 10 };

        db.MenuItems.AddRange(pHome, pAbout, pPlacements, pImpact, pLocations, pFees, pFaqs, pNews, pApply, pContact);
        db.SaveChanges(); // flush to get IDs for parent references

        db.MenuItems.AddRange(
            new MenuItem { TenantId = 0, Label = "Welcome to Negos",           Url = "/about",       IsVisible = true, Order = 1, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Why Negos?",                  Url = "/about#why",   IsVisible = true, Order = 2, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Why Volunteering in Nepal?", Url = "/our-impact",  IsVisible = true, Order = 3, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Get Involved",               Url = "/apply",       IsVisible = true, Order = 4, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Our Team",                   Url = "/about",       IsVisible = true, Order = 5, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Free Volunteering",          Url = "/fees",        IsVisible = true, Order = 6, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Nepali Host Families",       Url = "/about",       IsVisible = true, Order = 7, ParentId = pAbout.Id },
            new MenuItem { TenantId = 0, Label = "Volunteering",             Url = "/placements/volunteering",     IsVisible = true, Order = 1, ParentId = pPlacements.Id },
            new MenuItem { TenantId = 0, Label = "Internship",               Url = "/placements/internship",       IsVisible = true, Order = 2, ParentId = pPlacements.Id },
            new MenuItem { TenantId = 0, Label = "Nepal Experience Program", Url = "/placements/nepal-experience", IsVisible = true, Order = 3, ParentId = pPlacements.Id },
            new MenuItem { TenantId = 0, Label = "Nepali Language School",   Url = "/placements/language-school",  IsVisible = true, Order = 4, ParentId = pPlacements.Id },
            new MenuItem { TenantId = 0, Label = "Summer Volunteer Program", Url = "/placements/summer-program",   IsVisible = true, Order = 5, ParentId = pPlacements.Id },
            new MenuItem { TenantId = 0, Label = "Our Impact", Url = "/our-impact", IsVisible = true, Order = 1, ParentId = pImpact.Id },
            new MenuItem { TenantId = 0, Label = "Locations",  Url = "/locations",  IsVisible = true, Order = 2, ParentId = pImpact.Id },
            new MenuItem { TenantId = 0, Label = "All Locations", Url = "/locations",                IsVisible = true, Order = 1, ParentId = pLocations.Id },
            new MenuItem { TenantId = 0, Label = "Kathmandu",     Url = "/locations?city=Kathmandu", IsVisible = true, Order = 2, ParentId = pLocations.Id },
            new MenuItem { TenantId = 0, Label = "Pokhara",       Url = "/locations?city=Pokhara",   IsVisible = true, Order = 3, ParentId = pLocations.Id },
            new MenuItem { TenantId = 0, Label = "Rural Nepal",   Url = "/locations?city=Rural",     IsVisible = true, Order = 4, ParentId = pLocations.Id },
            new MenuItem { TenantId = 0, Label = "Program Fees",        Url = "/fees",                  IsVisible = true, Order = 1, ParentId = pFees.Id },
            new MenuItem { TenantId = 0, Label = "What's Included",     Url = "/whats-included",        IsVisible = true, Order = 2, ParentId = pFees.Id },
            new MenuItem { TenantId = 0, Label = "Payment & Booking",   Url = "/payment-booking",       IsVisible = true, Order = 3, ParentId = pFees.Id },
            new MenuItem { TenantId = 0, Label = "Charity Tour & Trek", Url = "/charity-tour-and-trek", IsVisible = true, Order = 4, ParentId = pFees.Id }
        );
        db.SaveChanges();
    }

    // -----------------------------------------------------------------------
    // Seed platform site settings (TenantId=0) if not already present
    // -----------------------------------------------------------------------
    if (!db.SiteSettings.Any(s => s.TenantId == 0))
    {
        var platformDefaults = new Dictionary<string, string>
        {
            ["platformName"]         = "Negos",
            ["platformTagline"]      = "The SaaS platform for volunteer organizations",
            ["platformLogoUrl"]      = "/logo.png",
            ["primaryColor"]         = "#e63946",
            ["secondaryColor"]       = "#457b9d",
            ["navbarColor"]          = "#ffffff",
            ["footerColor"]          = "#1d3557",
            ["buttonColor"]          = "#e63946",
            ["adminColor"]           = "#1d3557",
            ["address"]              = "Kathmandu, Nepal",
            ["phone"]                = "+977 9800000000",
            ["email"]                = "contact@negos.org",
            ["officeHours"]          = "Sun - Fri: 9am - 5pm",
            ["facebook"]             = "https://facebook.com",
            ["instagram"]            = "https://instagram.com",
            ["linkedin"]             = "https://linkedin.com",
            ["youtube"]              = "https://youtube.com",
            ["tiktok"]               = "https://tiktok.com",
            ["stat_volunteers"]      = "500+",
            ["stat_communities"]     = "20+",
            ["stat_livesImpacted"]   = "1000+",
            ["stat_yearsActive"]     = "10+",
            ["section_programs"]     = "true",
            ["section_news"]         = "true",
            ["section_tours"]        = "true",
            ["section_testimonials"] = "true",
            ["section_faqs"]         = "true",
            ["videoUrl"]             = "",
            ["videoTitle"]           = "Watch This Video To Know How Exciting Our Programs Are!",
            ["videoSubtitle"]        = "A glimpse of the volunteering journey in Nepal",
        };
        foreach (var kv in platformDefaults)
            db.SiteSettings.Add(new SiteSetting { TenantId = 0, Key = kv.Key, Value = kv.Value });
        db.SaveChanges();
    }
}
catch (Exception)
{
    // Seed warning — app still starts, data will be seeded on next restart
}

app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
