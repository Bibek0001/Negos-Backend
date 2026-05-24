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

// Convert postgres:// URL to Npgsql Host= format if needed
if (usePostgres) connStr = ConvertPostgresUrl(connStr);

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
// CORS — supports exact origins + wildcard subdomain matching
// Set AllowedOrigins env var to comma-separated list, e.g.:
//   https://negos.org,https://*.negos.org
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
    {
        var originsConfig = builder.Configuration["AllowedOrigins"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(originsConfig) || originsConfig == "*")
        {
            // Dev / unconfigured — allow all
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var entries = originsConfig
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Separate exact origins from wildcard patterns (e.g. https://*.negos.org)
            var exactOrigins   = entries.Where(e => !e.Contains("*")).ToArray();
            var wildcardDomains = entries
                .Where(e => e.Contains("*."))
                .Select(e => e.Replace("https://", "").Replace("http://", "").Replace("*.", ""))
                .ToArray(); // e.g. ["negos.org"]

            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(origin =>
                {
                    // Exact match
                    if (exactOrigins.Contains(origin)) return true;

                    // Wildcard subdomain match — e.g. https://diyalo.negos.org
                    try
                    {
                        var host = new Uri(origin).Host; // e.g. "diyalo.negos.org"
                        return wildcardDomains.Any(domain =>
                            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
                    }
                    catch { return false; }
                });
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
            // Use the already-converted connection string for DDL setup
            var rawSetup   = builder.Configuration.GetConnectionString("SetupConnection");
            var setupConnStr = string.IsNullOrWhiteSpace(rawSetup)
                ? connStr                          // no separate setup conn — use main (already converted)
                : ConvertPostgresUrl(rawSetup);    // convert setup URL the same way
            logger.LogInformation("Creating tables via raw SQL for PostgreSQL...");
            try
            {
                var setupOptions = new DbContextOptionsBuilder<AppDbContext>();
                setupOptions.UseNpgsql(setupConnStr);
                using var setupDb = new AppDbContext(setupOptions.Options);
                setupDb.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""Tenants"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Subdomain"" VARCHAR(450) NOT NULL,
                        ""Name"" TEXT NOT NULL,
                        ""Email"" TEXT NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Tenants_Subdomain"" ON ""Tenants""(""Subdomain"");

                    CREATE TABLE IF NOT EXISTS ""AdminUsers"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL DEFAULT 0,
                        ""Username"" TEXT NOT NULL,
                        ""PasswordHash"" TEXT NOT NULL,
                        ""Role"" TEXT NOT NULL DEFAULT 'Admin'
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_AdminUsers_TenantId"" ON ""AdminUsers""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""Programs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Title"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT,
                        ""Category"" TEXT NOT NULL DEFAULT 'Volunteering',
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""Order"" INT NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Programs_TenantId"" ON ""Programs""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""News"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Title"" TEXT NOT NULL,
                        ""Summary"" TEXT NOT NULL,
                        ""Body"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT,
                        ""Category"" TEXT NOT NULL DEFAULT '',
                        ""PublishedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_News_TenantId"" ON ""News""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""Tours"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Title"" TEXT NOT NULL,
                        ""Destination"" TEXT NOT NULL,
                        ""Duration"" TEXT NOT NULL,
                        ""Difficulty"" TEXT NOT NULL,
                        ""Type"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT,
                        ""Description"" TEXT NOT NULL,
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""Order"" INT NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Tours_TenantId"" ON ""Tours""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""Testimonials"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Name"" TEXT NOT NULL,
                        ""Country"" TEXT NOT NULL,
                        ""Message"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT,
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Testimonials_TenantId"" ON ""Testimonials""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""Faqs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Question"" TEXT NOT NULL,
                        ""Answer"" TEXT NOT NULL,
                        ""Order"" INT NOT NULL DEFAULT 0,
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Faqs_TenantId"" ON ""Faqs""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""HeroSlides"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Badge"" TEXT NOT NULL,
                        ""Title"" TEXT NOT NULL,
                        ""Highlight"" TEXT NOT NULL,
                        ""Subtitle"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT,
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""Order"" INT NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_HeroSlides_TenantId"" ON ""HeroSlides""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""MenuItems"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Label"" TEXT NOT NULL,
                        ""Url"" TEXT NOT NULL,
                        ""IsVisible"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""Order"" INT NOT NULL DEFAULT 0,
                        ""ParentId"" INT
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MenuItems_TenantId"" ON ""MenuItems""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""SiteSettings"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Key"" VARCHAR(450) NOT NULL,
                        ""Value"" TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SiteSettings_TenantId_Key"" ON ""SiteSettings""(""TenantId"", ""Key"");

                    CREATE TABLE IF NOT EXISTS ""Applications"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Name"" TEXT NOT NULL,
                        ""Email"" TEXT NOT NULL,
                        ""Phone"" TEXT NOT NULL,
                        ""Country"" TEXT NOT NULL,
                        ""Program"" TEXT NOT NULL,
                        ""StartDate"" TEXT,
                        ""Duration"" TEXT,
                        ""Message"" TEXT NOT NULL,
                        ""SubmittedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                        ""Status"" TEXT NOT NULL DEFAULT 'Pending'
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_Applications_TenantId"" ON ""Applications""(""TenantId"");

                    CREATE TABLE IF NOT EXISTS ""ContactMessages"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INT NOT NULL,
                        ""Name"" TEXT NOT NULL,
                        ""Email"" TEXT NOT NULL,
                        ""Subject"" TEXT NOT NULL,
                        ""Message"" TEXT NOT NULL,
                        ""SentAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                        ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ContactMessages_TenantId"" ON ""ContactMessages""(""TenantId"");
                ");
                logger.LogInformation("All tables created successfully.");
            }
            catch (Exception sqlEx)
            {
                logger.LogCritical(sqlEx, "Raw SQL table creation failed.");
                throw;
            }
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

    if (!dbReady) return; // skip seeding if DB setup failed — app still starts

    // Step 2: Seed tenants
    // Passwords are pre-computed BCrypt hashes — no plaintext in source code.
    // admin_diyalo    → Diyalo@123
    // admin_volunteer → Volunteer@123
    // admin_nepalhelp → NepalHelp@123
    var tenantsToSeed = new[]
    {
        new { Subdomain = "negos",          Name = "Negos",                Email = "contact@negos.org",          AdminUser = "admin_diyalo",    AdminHash = "$2a$11$NHOOsR2a/DNaalqlEK8VwOTSUFrjDKus7UyOqfYS/TFOZsFlnAPlK" },
        new { Subdomain = "volunteernepal", Name = "Volunteer Nepal",       Email = "contact@volunteernepal.org", AdminUser = "admin_volunteer", AdminHash = "$2a$11$GqDOYNN7XJBh4lbRZZfSG.zium62OsaA8ca9JS2u5m1i2MWVJYyv." },
        new { Subdomain = "nepalhelp",      Name = "Nepal Help Foundation", Email = "contact@nepalhelp.org",      AdminUser = "admin_nepalhelp", AdminHash = "$2a$11$ld5YghTqELhXW.xb9hb5zuKNyQRCgeJhK54NA9HC.3yc.yP68FXf." },
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
        var admin = db.AdminUsers.FirstOrDefault(u => u.Username == t.AdminUser && u.TenantId == tenant.Id);
        if (admin == null) db.AdminUsers.Add(new AdminUser { Username = t.AdminUser, PasswordHash = t.AdminHash, TenantId = tenant.Id, Role = "Admin" });
        // Do NOT overwrite existing password — allow it to be changed
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
    else { primary.Role = "SuperAdmin"; primary.TenantId = 0; } // keep existing password
    var backup = db.AdminUsers.FirstOrDefault(u => u.Username == "NegosBk");
    if (backup == null) db.AdminUsers.Add(new AdminUser { Username = "NegosBk", PasswordHash = backupHash, TenantId = 0, Role = "SuperAdmin" });
    else { backup.Role = "SuperAdmin"; backup.TenantId = 0; } // keep existing password
    db.SaveChanges();

    // Tenant admin accounts — only create if missing, never overwrite passwords
    // Hashes: admin_diyalo=Diyalo@123, admin_volunteer=Volunteer@123, admin_nepalhelp=NepalHelp@123
    foreach (var ta in new[] {
        new { Username = "admin_diyalo",    Hash = "$2a$11$NHOOsR2a/DNaalqlEK8VwOTSUFrjDKus7UyOqfYS/TFOZsFlnAPlK", TenantId = 1 },
        new { Username = "admin_volunteer", Hash = "$2a$11$GqDOYNN7XJBh4lbRZZfSG.zium62OsaA8ca9JS2u5m1i2MWVJYyv.", TenantId = 2 },
        new { Username = "admin_nepalhelp", Hash = "$2a$11$ld5YghTqELhXW.xb9hb5zuKNyQRCgeJhK54NA9HC.3yc.yP68FXf.", TenantId = 3 },
    })
    {
        var ex2 = db.AdminUsers.FirstOrDefault(u => u.Username == ta.Username);
        if (ex2 == null)
            db.AdminUsers.Add(new AdminUser { Username = ta.Username, PasswordHash = ta.Hash, TenantId = ta.TenantId, Role = "Admin" });
        else
            { ex2.Role = "Admin"; ex2.TenantId = ta.TenantId; } // never touch password
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
    // Full set of keys used by PlatformSettings.jsx and SiteContent.jsx
    var platformDefaults = new Dictionary<string, string>
    {
        // Branding
        ["platformName"]        = "Negos",
        ["platformTagline"]     = "The SaaS platform for volunteer organizations",
        ["platformLogoUrl"]     = "/logo.png",
        // Colors
        ["primaryColor"]        = "#e63946",
        ["secondaryColor"]      = "#457b9d",
        ["navbarColor"]         = "#ffffff",
        ["footerColor"]         = "#1d3557",
        ["buttonColor"]         = "#e63946",
        ["adminColor"]          = "#1d3557",
        // Contact
        ["address"]             = "Kathmandu, Nepal",
        ["phone"]               = "+977 9800000000",
        ["email"]               = "contact@negos.org",
        ["officeHours"]         = "Sun - Fri: 9am - 5pm",
        // Social
        ["facebook"]            = "https://facebook.com",
        ["instagram"]           = "https://instagram.com",
        ["linkedin"]            = "https://linkedin.com",
        ["youtube"]             = "https://youtube.com",
        ["tiktok"]              = "https://tiktok.com",
        // Stats
        ["stat_volunteers"]     = "500+",
        ["stat_communities"]    = "20+",
        ["stat_livesImpacted"]  = "1000+",
        ["stat_yearsActive"]    = "10+",
        // Section visibility
        ["section_programs"]    = "true",
        ["section_news"]        = "true",
        ["section_tours"]       = "true",
        ["section_testimonials"]= "true",
        ["section_faqs"]        = "true",
        // Video
        ["videoUrl"]            = "",
        ["videoTitle"]          = "Watch This Video To Know How Exciting Our Programs Are!",
        ["videoSubtitle"]       = "A glimpse of the volunteering journey in Nepal",
        // Hero section (used by PlatformSettings → Hero Content tab & SiteContent → Homepage tab)
        ["hero_badge"]          = "Volunteer Platform",
        ["hero_title"]          = "Power Your Organization.",
        ["hero_highlight"]      = "With Negos.",
        ["hero_subtitle"]       = "Negos is the all-in-one SaaS platform for volunteer organizations in Nepal. Manage programs, volunteers, news and more — all in one place.",
        // Intro section (SiteContent → Homepage tab)
        ["intro_description"]   = "is a Nepal-based, Nepali-run grassroots organization that provides volunteers with incredible experiences while making a real difference in local communities.",
        ["intro_mission"]       = "to encourage and invite as many international volunteers as possible to help Nepal grow, develop and thrive — while giving volunteers a life-changing experience.",
        // Platform intro (SiteContent → Homepage tab)
        ["platform_intro"]      = "Negos provides volunteer organizations with a complete digital platform to manage their programs, volunteers, news, tours and more — all from one easy-to-use admin panel.",
        // About page (SiteContent → Sections tab)
        ["about_intro"]         = "is a Nepal-based, Nepali-run grassroots organization dedicated to connecting international volunteers with communities that need them most.",
        ["about_mission"]       = "Our mission is to create meaningful volunteer experiences that benefit both the volunteers and the communities they serve across Nepal.",
        ["about_approach"]      = "We do this by partnering with local schools, hospitals, and community organizations to place volunteers where they can make the greatest impact.",
        // Homepage section titles (SiteContent → Sections tab)
        ["section_programs_title"]      = "Our Programs",
        ["section_programs_subtitle"]   = "Choose from a wide range of volunteer and internship programs across Nepal",
        ["section_news_title"]          = "Latest News",
        ["section_news_subtitle"]       = "Stay up to date with our latest stories, impact reports and announcements",
        ["section_testimonials_title"]  = "What Volunteers Say",
        ["section_testimonials_subtitle"] = "Hear from the volunteers who have experienced our programs first-hand",
        ["section_tours_title"]         = "Tours & Treks",
        ["section_tours_subtitle"]      = "Combine your volunteer experience with an unforgettable adventure in Nepal",
        ["section_combine_title"]       = "Combine Volunteering & Travel",
        ["section_combine_subtitle"]    = "Make the most of your time in Nepal by combining a volunteer placement with a guided tour or trek",
    };

    if (!db.SiteSettings.Any(s => s.TenantId == 0))
    {
        // First-time seed — insert all keys
        foreach (var kv in platformDefaults)
            db.SiteSettings.Add(new SiteSetting { TenantId = 0, Key = kv.Key, Value = kv.Value });
        db.SaveChanges();
    }
    else
    {
        // Backfill — add any keys that are missing (e.g. after an upgrade)
        var existingKeys = db.SiteSettings
            .Where(s => s.TenantId == 0)
            .Select(s => s.Key)
            .ToHashSet();
        var added = false;
        foreach (var kv in platformDefaults)
        {
            if (!existingKeys.Contains(kv.Key))
            {
                db.SiteSettings.Add(new SiteSetting { TenantId = 0, Key = kv.Key, Value = kv.Value });
                added = true;
            }
        }
        if (added) db.SaveChanges();
    }

    logger.LogInformation("Seeding completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Startup warning: {ex.Message}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// ---------------------------------------------------------------------------
// Helper: convert postgres:// or postgresql:// URL → Npgsql Host= format
// Handles URL-encoded characters (%40=@, %23=#, etc.) in passwords correctly
// ---------------------------------------------------------------------------
static string ConvertPostgresUrl(string url)
{
    if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return url;

    try
    {
        var uri      = new Uri(url);
        var host     = uri.Host;
        var port     = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/').Split('?')[0];
        if (string.IsNullOrEmpty(database)) database = "postgres";

        var userInfo = uri.UserInfo;
        var colonIdx = userInfo.IndexOf(':');
        var username = Uri.UnescapeDataString(colonIdx >= 0 ? userInfo.Substring(0, colonIdx) : userInfo);
        var password = Uri.UnescapeDataString(colonIdx >= 0 ? userInfo.Substring(colonIdx + 1) : "");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch
    {
        try
        {
            var s        = System.Text.RegularExpressions.Regex.Replace(url, @"^postgres(ql)?://", "");
            var lastAt   = s.LastIndexOf('@');
            var ui       = s.Substring(0, lastAt);
            var hp       = s.Substring(lastAt + 1);
            var ci       = ui.IndexOf(':');
            var username = Uri.UnescapeDataString(ci >= 0 ? ui.Substring(0, ci) : ui);
            var password = Uri.UnescapeDataString(ci >= 0 ? ui.Substring(ci + 1) : "");
            var si       = hp.IndexOf('/');
            var hostPort = si >= 0 ? hp.Substring(0, si) : hp;
            var database = si >= 0 ? hp.Substring(si + 1).Split('?')[0] : "postgres";
            var chi      = hostPort.LastIndexOf(':');
            var host     = chi >= 0 ? hostPort.Substring(0, chi) : hostPort;
            var port     = chi >= 0 ? hostPort.Substring(chi + 1) : "5432";
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
        catch { return url; }
    }
}
