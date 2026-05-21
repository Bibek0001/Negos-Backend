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
// Startup: DB setup + seed (fully non-fatal — app always starts)
// ---------------------------------------------------------------------------
try
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Step 1: Create database tables
    var dbReady = false;
    try
    {
        logger.LogInformation("Setting up database...");
        if (usePostgres || useSqlite)
        {
            // Drop the EF migrations history table if it exists from a previous
            // SQL Server migration attempt — it blocks EnsureCreated
            try
            {
                db.Database.ExecuteSqlRaw(
                    "DROP TABLE IF EXISTS \"__EFMigrationsHistory\"");
            }
            catch { /* ignore — table may not exist */ }

            // Create all tables from the current model (PostgreSQL-native types)
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }
        dbReady = true;
        logger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database setup failed. App starts with hardcoded login only.");
    }

    if (!dbReady) goto AppStart;

    // Step 2: Seed tenants
    var tenantsToSeed = new[]
    {
        new { Subdomain = "negos",          Name = "Negos",                Email = "contact@negos.org",          AdminUser = "admin_diyalo",    AdminPass = "Diyalo@123"    },
        new { Subdomain = "volunteernepal", Name = "Volunteer Nepal",       Email = "contact@volunteernepal.org", AdminUser = "admin_volunteer", AdminPass = "Volunteer@123" },
        new { Subdomain = "nepalhelp",      Name = "Nepal Help Foundation", Email = "contact@nepalhelp.org",      AdminUser = "admin_nepalhelp", AdminPass = "NepalHelp@123" },
    };

    foreach (var t in tenantsToSeed)
    {
        var tenant = db.Tenants.FirstOrDefault(x => x.Subdomain == t.Subdomain);
        if (tenant == null)
        {
            tenant = new Tenant { Subdomain = t.Subdomain, Name = t.Name, Email = t.Email, IsActive = true, CreatedAt = DateTime.UtcNow };
            db.Tenants.Add(tenant);
            db.SaveChanges();
            var prov = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
            await prov.ProvisionAsync(tenant);
        }
        else
        {
            var prov = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
            if (!db.MenuItems.Any(m => m.TenantId == tenant.Id))
                await prov.ProvisionAsync(tenant);
            else
                await prov.SeedContentIfMissingAsync(tenant);
        }
        var adminHash = BCrypt.Net.BCrypt.HashPassword(t.AdminPass);
        var admin = db.AdminUsers.FirstOrDefault(u => u.Username == t.AdminUser && u.TenantId == tenant.Id);
        if (admin == null) db.AdminUsers.Add(new AdminUser { Username = t.AdminUser, PasswordHash = adminHash, TenantId = tenant.Id, Role = "Admin" });
        else { admin.PasswordHash = adminHash; admin.Role = "Admin"; admin.TenantId = tenant.Id; }
    }
    db.SaveChanges();

    // Remove stale accounts
    var stale = db.AdminUsers.Where(u => u.Username == "admin_negos").ToList();
    if (stale.Any()) { db.AdminUsers.RemoveRange(stale); db.SaveChanges(); }

    // SuperAdmin accounts
    const string primaryHash = "$2a$11$d2otSTC/OPK.lifVGrU0iuqLja245JkNirM92Vp7l7kbfKN3ArUcy";
    const string backupHash  = "$2a$11$EewzvCHCXARXpWrEFtkVWuew7MyK8bS0qOCma96ZCY7tozIGNc4Dm";
    var primary = db.AdminUsers.FirstOrDefault(u => u.Username == "Negos");
    if (primary == null) db.AdminUsers.Add(new AdminUser { Username = "Negos", PasswordHash = primaryHash, TenantId = 0, Role = "SuperAdmin" });
    else { primary.PasswordHash = primaryHash; primary.Role = "SuperAdmin"; primary.TenantId = 0; }
    var backup = db.AdminUsers.FirstOrDefault(u => u.Username == "NegosBk");
    if (backup == null) db.AdminUsers.Add(new AdminUser { Username = "NegosBk", PasswordHash = backupHash, TenantId = 0, Role = "SuperAdmin" });
    else { backup.PasswordHash = backupHash; backup.Role = "SuperAdmin"; backup.TenantId = 0; }
    db.SaveChanges();

    // Tenant admin accounts
    foreach (var ta in new[] {
        new { Username = "admin_diyalo",    Password = "Diyalo@123",    TenantId = 1 },
        new { Username = "admin_volunteer", Password = "Volunteer@123", TenantId = 2 },
        new { Username = "admin_nepalhelp", Password = "NepalHelp@123", TenantId = 3 },
    })
    {
        var h = BCrypt.Net.BCrypt.HashPassword(ta.Password);
        var ex2 = db.AdminUsers.FirstOrDefault(u => u.Username == ta.Username);
        if (ex2 == null) db.AdminUsers.Add(new AdminUser { Username = ta.Username, PasswordHash = h, TenantId = ta.TenantId, Role = "Admin" });
        else { ex2.PasswordHash = h; ex2.Role = "Admin"; ex2.TenantId = ta.TenantId; }
    }
    db.SaveChanges();

    // Platform menu items (TenantId=0)
    if (!db.MenuItems.Any(m => m.TenantId == 0))
    {
        var pH=new MenuItem{TenantId=0,Label="Home",      Url="/",                       IsVisible=true,Order=1};
        var pA=new MenuItem{TenantId=0,Label="About Us",  Url="/about",                  IsVisible=true,Order=2};
        var pP=new MenuItem{TenantId=0,Label="Placements",Url="/placements/volunteering",IsVisible=true,Order=3};
        var pI=new MenuItem{TenantId=0,Label="Our Impact",Url="/our-impact",             IsVisible=true,Order=4};
        var pL=new MenuItem{TenantId=0,Label="Locations", Url="/locations",              IsVisible=true,Order=5};
        var pF=new MenuItem{TenantId=0,Label="Fees",      Url="/fees",                   IsVisible=true,Order=6};
        var pQ=new MenuItem{TenantId=0,Label="FAQs",      Url="/faqs",                   IsVisible=true,Order=7};
        var pN=new MenuItem{TenantId=0,Label="News",      Url="/news",                   IsVisible=true,Order=8};
        var pAp=new MenuItem{TenantId=0,Label="Apply Now",Url="/apply",                  IsVisible=true,Order=9};
        var pC=new MenuItem{TenantId=0,Label="Contact Us",Url="/contact",                IsVisible=true,Order=10};
        db.MenuItems.AddRange(pH,pA,pP,pI,pL,pF,pQ,pN,pAp,pC);
        db.SaveChanges();
        db.MenuItems.AddRange(
            new MenuItem{TenantId=0,Label="Welcome to Negos",          Url="/about",                       IsVisible=true,Order=1,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Why Negos?",                Url="/about#why",                   IsVisible=true,Order=2,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Why Volunteering in Nepal?",Url="/our-impact",                  IsVisible=true,Order=3,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Get Involved",              Url="/apply",                       IsVisible=true,Order=4,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Our Team",                  Url="/about",                       IsVisible=true,Order=5,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Free Volunteering",         Url="/fees",                        IsVisible=true,Order=6,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Nepali Host Families",      Url="/about",                       IsVisible=true,Order=7,ParentId=pA.Id},
            new MenuItem{TenantId=0,Label="Volunteering",             Url="/placements/volunteering",      IsVisible=true,Order=1,ParentId=pP.Id},
            new MenuItem{TenantId=0,Label="Internship",               Url="/placements/internship",        IsVisible=true,Order=2,ParentId=pP.Id},
            new MenuItem{TenantId=0,Label="Nepal Experience Program",  Url="/placements/nepal-experience", IsVisible=true,Order=3,ParentId=pP.Id},
            new MenuItem{TenantId=0,Label="Nepali Language School",    Url="/placements/language-school",  IsVisible=true,Order=4,ParentId=pP.Id},
            new MenuItem{TenantId=0,Label="Summer Volunteer Program",  Url="/placements/summer-program",   IsVisible=true,Order=5,ParentId=pP.Id},
            new MenuItem{TenantId=0,Label="Our Impact",Url="/our-impact",IsVisible=true,Order=1,ParentId=pI.Id},
            new MenuItem{TenantId=0,Label="Locations", Url="/locations", IsVisible=true,Order=2,ParentId=pI.Id},
            new MenuItem{TenantId=0,Label="All Locations",Url="/locations",               IsVisible=true,Order=1,ParentId=pL.Id},
            new MenuItem{TenantId=0,Label="Kathmandu",   Url="/locations?city=Kathmandu", IsVisible=true,Order=2,ParentId=pL.Id},
            new MenuItem{TenantId=0,Label="Pokhara",     Url="/locations?city=Pokhara",   IsVisible=true,Order=3,ParentId=pL.Id},
            new MenuItem{TenantId=0,Label="Rural Nepal", Url="/locations?city=Rural",     IsVisible=true,Order=4,ParentId=pL.Id},
            new MenuItem{TenantId=0,Label="Program Fees",       Url="/fees",                  IsVisible=true,Order=1,ParentId=pF.Id},
            new MenuItem{TenantId=0,Label="What's Included",    Url="/whats-included",        IsVisible=true,Order=2,ParentId=pF.Id},
            new MenuItem{TenantId=0,Label="Payment & Booking",  Url="/payment-booking",       IsVisible=true,Order=3,ParentId=pF.Id},
            new MenuItem{TenantId=0,Label="Charity Tour & Trek",Url="/charity-tour-and-trek", IsVisible=true,Order=4,ParentId=pF.Id}
        );
        db.SaveChanges();
    }

    // Platform site settings (TenantId=0)
    if (!db.SiteSettings.Any(s => s.TenantId == 0))
    {
        foreach (var kv in new Dictionary<string,string>{
            ["platformName"]="Negos",["platformTagline"]="The SaaS platform for volunteer organizations",
            ["platformLogoUrl"]="/logo.png",["primaryColor"]="#e63946",["secondaryColor"]="#457b9d",
            ["navbarColor"]="#ffffff",["footerColor"]="#1d3557",["buttonColor"]="#e63946",["adminColor"]="#1d3557",
            ["address"]="Kathmandu, Nepal",["phone"]="+977 9800000000",["email"]="contact@negos.org",
            ["officeHours"]="Sun - Fri: 9am - 5pm",["facebook"]="https://facebook.com",
            ["instagram"]="https://instagram.com",["linkedin"]="https://linkedin.com",
            ["youtube"]="https://youtube.com",["tiktok"]="https://tiktok.com",
            ["stat_volunteers"]="500+",["stat_communities"]="20+",["stat_livesImpacted"]="1000+",["stat_yearsActive"]="10+",
            ["section_programs"]="true",["section_news"]="true",["section_tours"]="true",
            ["section_testimonials"]="true",["section_faqs"]="true",
            ["videoUrl"]="",["videoTitle"]="Watch This Video To Know How Exciting Our Programs Are!",
            ["videoSubtitle"]="A glimpse of the volunteering journey in Nepal",
        })
            db.SiteSettings.Add(new SiteSetting{TenantId=0,Key=kv.Key,Value=kv.Value});
        db.SaveChanges();
    }

    logger.LogInformation("Seeding completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Startup warning: {ex.Message}");
}

AppStart:
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
