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

    public async Task SeedContentIfMissingAsync(Tenant tenant)
    {
        var tid  = tenant.Id;
        var name = tenant.Name;

        // Programs
        if (!_db.Programs.Any(p => p.TenantId == tid))
        {
            _db.Programs.AddRange(
                new VolunteerProgram { TenantId = tid, Title = "Teaching Volunteer",       Category = "Volunteering",     IsVisible = true, Order = 1, Description = "Teach English, Math and Science to children in schools across Nepal. Make a real difference in the lives of students who need quality education." },
                new VolunteerProgram { TenantId = tid, Title = "Medical Elective",         Category = "Internship",       IsVisible = true, Order = 2, Description = "Gain hands-on clinical experience working alongside Nepali medical staff in hospitals and community health centers." },
                new VolunteerProgram { TenantId = tid, Title = "Construction Work",        Category = "Volunteering",     IsVisible = true, Order = 3, Description = "Help rebuild schools and community centers. No experience needed — our local team guides you through every task." },
                new VolunteerProgram { TenantId = tid, Title = "Child Care",               Category = "Volunteering",     IsVisible = true, Order = 4, Description = "Work with children in orphanages and care centers, providing educational activities and emotional support." },
                new VolunteerProgram { TenantId = tid, Title = "Women's Empowerment",      Category = "Volunteering",     IsVisible = true, Order = 5, Description = "Teach vocational skills, English and financial management to women from disadvantaged backgrounds." },
                new VolunteerProgram { TenantId = tid, Title = "Physiotherapy Internship", Category = "Internship",       IsVisible = true, Order = 6, Description = "Help rehabilitate children and elderly patients in hospitals and community clinics across Nepal." },
                new VolunteerProgram { TenantId = tid, Title = "Nepal Experience Program", Category = "Nepal Experience", IsVisible = true, Order = 7, Description = "Immerse yourself in Nepali culture, language and community life while contributing to meaningful projects." },
                new VolunteerProgram { TenantId = tid, Title = "Nepali Language School",   Category = "Language School",  IsVisible = true, Order = 8, Description = "Learn the Nepali language and culture in an immersive environment with experienced local teachers." },
                new VolunteerProgram { TenantId = tid, Title = "Summer Volunteer Program", Category = "Summer Program",   IsVisible = true, Order = 9, Description = "A short-term intensive volunteer experience perfect for students and professionals during summer break." }
            );
        }

        // News
        if (!_db.News.Any(n => n.TenantId == tid))
        {
            _db.News.AddRange(
                new News { TenantId = tid, Title = $"{name} Welcomes New Volunteers for 2026", Category = "Announcement", Summary = "We are excited to welcome a new batch of international volunteers joining our programs this year.", Body = $"{name} is thrilled to announce the arrival of our newest cohort of international volunteers. This year we have volunteers from over 15 countries joining our teaching, medical, and construction programs across Nepal.", PublishedAt = DateTime.UtcNow.AddDays(-5) },
                new News { TenantId = tid, Title = "New School Built in Sindhupalchok",         Category = "Impact",        Summary = "Thanks to our volunteers, a new school has been completed in the earthquake-affected Sindhupalchok district.", Body = "After months of hard work by our dedicated volunteers and local community members, we are proud to announce the completion of a new school building in Sindhupalchok. The school will serve over 200 children.", PublishedAt = DateTime.UtcNow.AddDays(-15) },
                new News { TenantId = tid, Title = "Medical Camp Reaches 500 Patients",         Category = "Health",        Summary = "Our volunteer doctors and nurses provided free medical care to over 500 patients in rural Nepal.", Body = "Our recent medical camp in the remote hills of Nepal was a tremendous success. Volunteer doctors, nurses and medical students provided free consultations and treatments to over 500 patients.", PublishedAt = DateTime.UtcNow.AddDays(-30) }
            );
        }

        // Tours
        if (!_db.Tours.Any(t => t.TenantId == tid))
        {
            _db.Tours.AddRange(
                new Tour { TenantId = tid, Title = "Everest Base Camp Trek",   Destination = "Nepal", Duration = "14 Days", Difficulty = "Hard",     Type = "Trekking + Volunteer", IsVisible = true, Order = 1, Description = "Trek to the base of the world's highest mountain while contributing to local communities along the way." },
                new Tour { TenantId = tid, Title = "Annapurna Circuit",        Destination = "Nepal", Duration = "12 Days", Difficulty = "Moderate", Type = "Trekking",             IsVisible = true, Order = 2, Description = "One of the world's greatest treks, circling the Annapurna massif through diverse landscapes and cultures." },
                new Tour { TenantId = tid, Title = "Kathmandu Cultural Tour",  Destination = "Nepal", Duration = "5 Days",  Difficulty = "Easy",     Type = "Tour",                 IsVisible = true, Order = 3, Description = "Explore the ancient temples, palaces and vibrant streets of Kathmandu Valley." },
                new Tour { TenantId = tid, Title = "Pokhara & Poon Hill Trek", Destination = "Nepal", Duration = "8 Days",  Difficulty = "Moderate", Type = "Tour + Volunteer",     IsVisible = true, Order = 4, Description = "Combine the stunning beauty of Pokhara with a trek to Poon Hill for breathtaking Himalayan sunrise views." },
                new Tour { TenantId = tid, Title = "Chitwan Jungle Safari",    Destination = "Nepal", Duration = "4 Days",  Difficulty = "Easy",     Type = "Tour",                 IsVisible = true, Order = 5, Description = "Experience Nepal's incredible wildlife in Chitwan National Park." }
            );
        }

        // Testimonials
        if (!_db.Testimonials.Any(t => t.TenantId == tid))
        {
            _db.Testimonials.AddRange(
                new Testimonial { TenantId = tid, Name = "Sarah Johnson",    Country = "United Kingdom", Message = "An absolutely life-changing experience. The children were so eager to learn and the local team was incredibly supportive.", IsVisible = true },
                new Testimonial { TenantId = tid, Name = "Marcus Weber",     Country = "Germany",        Message = "As a medical student, this placement gave me invaluable hands-on experience. Working in a resource-limited setting taught me so much.", IsVisible = true },
                new Testimonial { TenantId = tid, Name = "Emma Thompson",    Country = "Australia",      Message = "I built a school with my own hands. Seeing the children use the classroom we constructed was the most rewarding moment of my life.", IsVisible = true },
                new Testimonial { TenantId = tid, Name = "Carlos Rodriguez", Country = "Spain",          Message = "The Nepal Experience Program was perfect. I learned the language, made lifelong friends and truly understood Nepali culture.", IsVisible = true },
                new Testimonial { TenantId = tid, Name = "Yuki Tanaka",      Country = "Japan",          Message = "Working with the children was deeply moving. Despite language barriers, we connected through play and laughter.", IsVisible = true }
            );
        }

        await _db.SaveChangesAsync();
    }

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

        // -----------------------------------------------------------------------
        // Default Programs
        // -----------------------------------------------------------------------
        _db.Programs.AddRange(
            new VolunteerProgram { TenantId = tid, Title = "Teaching Volunteer",        Category = "Volunteering", IsVisible = true, Order = 1, Description = "Teach English, Math and Science to children in schools across Nepal. Make a real difference in the lives of students who need quality education.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Medical Elective",          Category = "Internship",   IsVisible = true, Order = 2, Description = "Gain hands-on clinical experience working alongside Nepali medical staff in hospitals and community health centers.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Construction Work",         Category = "Volunteering", IsVisible = true, Order = 3, Description = "Help rebuild schools and community centers. No experience needed — our local team guides you through every task.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Child Care",                Category = "Volunteering", IsVisible = true, Order = 4, Description = "Work with children in orphanages and care centers, providing educational activities and emotional support.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Women's Empowerment",       Category = "Volunteering", IsVisible = true, Order = 5, Description = "Teach vocational skills, English and financial management to women from disadvantaged backgrounds.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Physiotherapy Internship",  Category = "Internship",   IsVisible = true, Order = 6, Description = "Help rehabilitate children and elderly patients in hospitals and community clinics across Nepal.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Nepal Experience Program",  Category = "Nepal Experience", IsVisible = true, Order = 7, Description = "Immerse yourself in Nepali culture, language and community life while contributing to meaningful projects.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Nepali Language School",    Category = "Language School",  IsVisible = true, Order = 8, Description = "Learn the Nepali language and culture in an immersive environment with experienced local teachers.", ImageUrl = "" },
            new VolunteerProgram { TenantId = tid, Title = "Summer Volunteer Program",  Category = "Summer Program",   IsVisible = true, Order = 9, Description = "A short-term intensive volunteer experience perfect for students and professionals during summer break.", ImageUrl = "" }
        );

        // -----------------------------------------------------------------------
        // Default News
        // -----------------------------------------------------------------------
        _db.News.AddRange(
            new News { TenantId = tid, Title = $"{name} Welcomes New Volunteers for 2026", Category = "Announcement", Summary = "We are excited to welcome a new batch of international volunteers joining our programs this year.", Body = $"{name} is thrilled to announce the arrival of our newest cohort of international volunteers. This year we have volunteers from over 15 countries joining our teaching, medical, and construction programs across Nepal. Their dedication and passion continue to inspire our local communities.", PublishedAt = DateTime.UtcNow.AddDays(-5) },
            new News { TenantId = tid, Title = "New School Built in Sindhupalchok",         Category = "Impact",        Summary = "Thanks to our volunteers, a new school has been completed in the earthquake-affected Sindhupalchok district.", Body = "After months of hard work by our dedicated volunteers and local community members, we are proud to announce the completion of a new school building in Sindhupalchok. The school will serve over 200 children who previously had to travel long distances for education.", PublishedAt = DateTime.UtcNow.AddDays(-15) },
            new News { TenantId = tid, Title = "Medical Camp Reaches 500 Patients",         Category = "Health",        Summary = "Our volunteer doctors and nurses provided free medical care to over 500 patients in rural Nepal.", Body = "Our recent medical camp in the remote hills of Nepal was a tremendous success. Volunteer doctors, nurses and medical students provided free consultations, medicines and basic treatments to over 500 patients who have limited access to healthcare facilities.", PublishedAt = DateTime.UtcNow.AddDays(-30) }
        );

        // -----------------------------------------------------------------------
        // Default Tours
        // -----------------------------------------------------------------------
        _db.Tours.AddRange(
            new Tour { TenantId = tid, Title = "Everest Base Camp Trek",      Destination = "Nepal", Duration = "14 Days", Difficulty = "Hard",     Type = "Trekking + Volunteer", IsVisible = true, Order = 1, Description = "Trek to the base of the world's highest mountain while contributing to local communities along the way. An unforgettable adventure combining trekking and volunteering." },
            new Tour { TenantId = tid, Title = "Annapurna Circuit",           Destination = "Nepal", Duration = "12 Days", Difficulty = "Moderate", Type = "Trekking",             IsVisible = true, Order = 2, Description = "One of the world's greatest treks, circling the Annapurna massif through diverse landscapes, cultures and climates." },
            new Tour { TenantId = tid, Title = "Kathmandu Cultural Tour",     Destination = "Nepal", Duration = "5 Days",  Difficulty = "Easy",     Type = "Tour",                 IsVisible = true, Order = 3, Description = "Explore the ancient temples, palaces and vibrant streets of Kathmandu Valley. Visit UNESCO World Heritage Sites and experience authentic Nepali culture." },
            new Tour { TenantId = tid, Title = "Pokhara & Poon Hill Trek",    Destination = "Nepal", Duration = "8 Days",  Difficulty = "Moderate", Type = "Tour + Volunteer",     IsVisible = true, Order = 4, Description = "Combine the stunning beauty of Pokhara with a trek to Poon Hill for breathtaking Himalayan sunrise views, plus a day of community volunteering." },
            new Tour { TenantId = tid, Title = "Chitwan Jungle Safari",       Destination = "Nepal", Duration = "4 Days",  Difficulty = "Easy",     Type = "Tour",                 IsVisible = true, Order = 5, Description = "Experience Nepal's incredible wildlife in Chitwan National Park. Spot rhinos, elephants, crocodiles and if lucky, the elusive Bengal tiger." }
        );

        // -----------------------------------------------------------------------
        // Default Testimonials
        // -----------------------------------------------------------------------
        _db.Testimonials.AddRange(
            new Testimonial { TenantId = tid, Name = "Sarah Johnson",    Country = "United Kingdom", Message = "An absolutely life-changing experience. The children were so eager to learn and the local team was incredibly supportive. I came to give but received so much more in return.", IsVisible = true },
            new Testimonial { TenantId = tid, Name = "Marcus Weber",     Country = "Germany",        Message = "As a medical student, this placement gave me invaluable hands-on experience I could never get at home. Working in a resource-limited setting taught me so much about adaptability and compassion.", IsVisible = true },
            new Testimonial { TenantId = tid, Name = "Emma Thompson",    Country = "Australia",      Message = "I built a school with my own hands. Seeing the children use the classroom we constructed was the most rewarding moment of my life. Highly recommend to anyone wanting to make a real impact.", IsVisible = true },
            new Testimonial { TenantId = tid, Name = "Carlos Rodriguez", Country = "Spain",          Message = "The Nepal Experience Program was perfect for me. I learned the language, made lifelong friends and truly understood Nepali culture. The organization was professional and caring throughout.", IsVisible = true },
            new Testimonial { TenantId = tid, Name = "Yuki Tanaka",      Country = "Japan",          Message = "Working with the children at the care center was deeply moving. Despite language barriers, we connected through play and laughter. This experience has changed my perspective on life forever.", IsVisible = true }
        );

        await _db.SaveChangesAsync();
    }
}
