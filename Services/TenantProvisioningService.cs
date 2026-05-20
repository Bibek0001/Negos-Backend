using Diyalo.Api.Data;
using Diyalo.Api.Models;

namespace Diyalo.Api.Services;

/// <summary>
/// Provisions default data for a newly created tenant.
/// Called once when a tenant is created via the Super Admin panel.
/// Seeds: MenuItems, HeroSlides, SiteSettings, FAQs
/// </summary>
public class TenantProvisioningService
{
    private readonly AppDbContext _db;

    public TenantProvisioningService(AppDbContext db) => _db = db;

    public async Task ProvisionAsync(Tenant tenant)
    {
        var tid  = tenant.Id;
        var name = tenant.Name;

        // -----------------------------------------------------------------------
        // Menu Items (top-level)
        // -----------------------------------------------------------------------
        var home       = new MenuItem { TenantId = tid, Label = "Home",       Url = "/",                        IsVisible = true, Order = 1  };
        var about      = new MenuItem { TenantId = tid, Label = "About Us",   Url = "/about",                   IsVisible = true, Order = 2  };
        var placements = new MenuItem { TenantId = tid, Label = "Placements", Url = "/placements/volunteering", IsVisible = true, Order = 3  };
        var impact     = new MenuItem { TenantId = tid, Label = "Our Impact", Url = "/our-impact",              IsVisible = true, Order = 4  };
        var locations  = new MenuItem { TenantId = tid, Label = "Locations",  Url = "/locations",               IsVisible = true, Order = 5  };
        var fees       = new MenuItem { TenantId = tid, Label = "Fees",       Url = "/fees",                    IsVisible = true, Order = 6  };
        var faqs       = new MenuItem { TenantId = tid, Label = "FAQs",       Url = "/faqs",                    IsVisible = true, Order = 7  };
        var news       = new MenuItem { TenantId = tid, Label = "News",       Url = "/news",                    IsVisible = true, Order = 8  };
        var apply      = new MenuItem { TenantId = tid, Label = "Apply Now",  Url = "/apply",                   IsVisible = true, Order = 9  };
        var contact    = new MenuItem { TenantId = tid, Label = "Contact Us", Url = "/contact",                 IsVisible = true, Order = 10 };

        _db.MenuItems.AddRange(home, about, placements, impact, locations, fees, faqs, news, apply, contact);
        await _db.SaveChangesAsync(); // flush so we get IDs for parent references

        // -----------------------------------------------------------------------
        // Submenu Items (children — ParentId references the top-level items above)
        // -----------------------------------------------------------------------
        _db.MenuItems.AddRange(
            // About Us
            new MenuItem { TenantId = tid, Label = "Welcome to Negos",           Url = "/about",       IsVisible = true, Order = 1, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Why Negos?",                 Url = "/about#why",   IsVisible = true, Order = 2, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Why Volunteering in Nepal?", Url = "/our-impact",  IsVisible = true, Order = 3, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Get Involved",               Url = "/apply",       IsVisible = true, Order = 4, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Our Team",                   Url = "/about",       IsVisible = true, Order = 5, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Free Volunteering",          Url = "/fees",        IsVisible = true, Order = 6, ParentId = about.Id },
            new MenuItem { TenantId = tid, Label = "Nepali Host Families",       Url = "/about",       IsVisible = true, Order = 7, ParentId = about.Id },

            // Placements
            new MenuItem { TenantId = tid, Label = "Volunteering",             Url = "/placements/volunteering",    IsVisible = true, Order = 1, ParentId = placements.Id },
            new MenuItem { TenantId = tid, Label = "Internship",               Url = "/placements/internship",      IsVisible = true, Order = 2, ParentId = placements.Id },
            new MenuItem { TenantId = tid, Label = "Nepal Experience Program", Url = "/placements/nepal-experience", IsVisible = true, Order = 3, ParentId = placements.Id },
            new MenuItem { TenantId = tid, Label = "Nepali Language School",   Url = "/placements/language-school",  IsVisible = true, Order = 4, ParentId = placements.Id },
            new MenuItem { TenantId = tid, Label = "Summer Volunteer Program", Url = "/placements/summer-program",   IsVisible = true, Order = 5, ParentId = placements.Id },

            // Our Impact
            new MenuItem { TenantId = tid, Label = "Our Impact", Url = "/our-impact", IsVisible = true, Order = 1, ParentId = impact.Id },
            new MenuItem { TenantId = tid, Label = "Locations",  Url = "/locations",  IsVisible = true, Order = 2, ParentId = impact.Id },

            // Locations
            new MenuItem { TenantId = tid, Label = "All Locations", Url = "/locations",                IsVisible = true, Order = 1, ParentId = locations.Id },
            new MenuItem { TenantId = tid, Label = "Kathmandu",     Url = "/locations?city=Kathmandu", IsVisible = true, Order = 2, ParentId = locations.Id },
            new MenuItem { TenantId = tid, Label = "Pokhara",       Url = "/locations?city=Pokhara",   IsVisible = true, Order = 3, ParentId = locations.Id },
            new MenuItem { TenantId = tid, Label = "Rural Nepal",   Url = "/locations?city=Rural",     IsVisible = true, Order = 4, ParentId = locations.Id },

            // Fees
            new MenuItem { TenantId = tid, Label = "Program Fees",        Url = "/fees",                  IsVisible = true, Order = 1, ParentId = fees.Id },
            new MenuItem { TenantId = tid, Label = "What's Included",     Url = "/whats-included",        IsVisible = true, Order = 2, ParentId = fees.Id },
            new MenuItem { TenantId = tid, Label = "Payment & Booking",   Url = "/payment-booking",       IsVisible = true, Order = 3, ParentId = fees.Id },
            new MenuItem { TenantId = tid, Label = "Charity Tour & Trek", Url = "/charity-tour-and-trek", IsVisible = true, Order = 4, ParentId = fees.Id }
        );

        // -----------------------------------------------------------------------
        // Hero Slides
        // -----------------------------------------------------------------------
        _db.HeroSlides.AddRange(
            new HeroSlide { TenantId = tid, Badge = "Volunteer in Nepal",  Title = "Light the Way.",   Highlight = "Change a Life.",   Subtitle = $"{name} connects passionate volunteers with communities that need them most.", Order = 1, IsVisible = true },
            new HeroSlide { TenantId = tid, Badge = "Make an Impact",      Title = "Help Rebuild",     Highlight = "Nepal.",           Subtitle = "Join our construction and community development programs across Nepal.",        Order = 2, IsVisible = true },
            new HeroSlide { TenantId = tid, Badge = "Teach & Inspire",     Title = "Educate a Child.", Highlight = "Shape the Future.", Subtitle = "Volunteer as a teacher and give children the gift of education.",             Order = 3, IsVisible = true }
        );

        // -----------------------------------------------------------------------
        // Site Settings
        // -----------------------------------------------------------------------
        var settings = new Dictionary<string, string>
        {
            ["siteName"]            = name,
            ["address"]             = "Kathmandu, Nepal",
            ["phone"]               = "+977 9800000000",
            ["email"]               = tenant.Email,
            ["facebook"]            = "https://facebook.com",
            ["instagram"]           = "https://instagram.com",
            ["linkedin"]            = "https://linkedin.com",
            ["youtube"]             = "https://youtube.com",
            ["tiktok"]              = "https://tiktok.com",
            ["officeHours"]         = "Sun - Fri: 9am - 5pm",
            ["stat_volunteers"]     = "500+",
            ["stat_communities"]    = "20+",
            ["stat_livesImpacted"]  = "1000+",
            ["stat_yearsActive"]    = "10+",
            ["logoUrl"]             = "",
            ["primaryColor"]        = "#e63946",
            ["secondaryColor"]      = "#457b9d",
            ["navbarColor"]         = "#ffffff",
            ["footerColor"]         = "#1d3557",
            ["buttonColor"]         = "#e63946",
            ["videoUrl"]            = "",
            ["videoTitle"]          = "Watch This Video To Know How Exciting Our Programs Are!",
            ["videoSubtitle"]       = "A glimpse of the volunteering journey in Nepal",
            ["section_programs"]    = "true",
            ["section_news"]        = "true",
            ["section_tours"]       = "true",
            ["section_testimonials"]= "true",
            ["section_faqs"]        = "true",
        };

        foreach (var kv in settings)
            _db.SiteSettings.Add(new SiteSetting { TenantId = tid, Key = kv.Key, Value = kv.Value });

        // -----------------------------------------------------------------------
        // Default FAQs
        // -----------------------------------------------------------------------
        _db.Faqs.AddRange(
            new Faq { TenantId = tid, Question = "How do I apply to volunteer?",        Answer = "Fill out our online application form on the Apply Now page. Our team will review your application and get back to you within 3-5 business days.", Order = 1, IsVisible = true },
            new Faq { TenantId = tid, Question = "What is the minimum age to volunteer?", Answer = "The minimum age is 18 years old for most programs.",                                                                                              Order = 2, IsVisible = true },
            new Faq { TenantId = tid, Question = "How long can I volunteer?",            Answer = "We offer flexible durations from 2 weeks to 3 months.",                                                                                           Order = 3, IsVisible = true },
            new Faq { TenantId = tid, Question = "What is included in the program fee?", Answer = "The fee includes accommodation, meals, airport pickup, orientation, program placement, 24/7 local support, and a certificate of completion.",     Order = 4, IsVisible = true },
            new Faq { TenantId = tid, Question = "Do I need to speak Nepali?",           Answer = "No, English is widely spoken in our programs.",                                                                                                   Order = 5, IsVisible = true }
        );

        await _db.SaveChangesAsync();
    }
}
