-- =============================================================================
-- Diyalo — Production Seed Script (Multi-Tenant)
-- Run this ONCE on the production server AFTER the app has started for the
-- first time (so EF migrations have already created all tables).
--
-- Usage:
--   sqlcmd -S YOUR_SERVER -d DiyaloDB -i seed.sql
-- =============================================================================

SET NOCOUNT ON;
GO

-- =============================================================================
-- STEP 1: Create Tenant 1 (Diyalo Nepal)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Tenants ON;
    INSERT INTO Tenants (Id, Subdomain, Name, Email, IsActive, CreatedAt)
    VALUES (1, 'diyalo', 'Diyalo Nepal', 'contact@diyalo.org', 1, GETUTCDATE());
    SET IDENTITY_INSERT Tenants OFF;
    PRINT 'Tenant 1 created.';
END
ELSE
    PRINT 'Tenant 1 already exists — skipped.';
GO

-- =============================================================================
-- STEP 2: Migrate existing data to TenantId = 1
-- (Only needed if upgrading from single-tenant version)
-- =============================================================================
UPDATE Programs        SET TenantId = 1 WHERE TenantId = 0;
UPDATE News            SET TenantId = 1 WHERE TenantId = 0;
UPDATE Tours           SET TenantId = 1 WHERE TenantId = 0;
UPDATE Testimonials    SET TenantId = 1 WHERE TenantId = 0;
UPDATE Faqs            SET TenantId = 1 WHERE TenantId = 0;
UPDATE HeroSlides      SET TenantId = 1 WHERE TenantId = 0;
UPDATE MenuItems       SET TenantId = 1 WHERE TenantId = 0;
UPDATE SiteSettings    SET TenantId = 1 WHERE TenantId = 0;
UPDATE Applications    SET TenantId = 1 WHERE TenantId = 0;
UPDATE ContactMessages SET TenantId = 1 WHERE TenantId = 0;
UPDATE AdminUsers      SET TenantId = 0, Role = 'SuperAdmin' WHERE Username IN ('Negos', 'NegosBk');
PRINT 'Existing data migrated to TenantId=1.';
GO

-- =============================================================================
-- MENU ITEMS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE TenantId = 1)
BEGIN
    INSERT INTO MenuItems (TenantId, Label, Url, IsVisible, [Order]) VALUES
    (1, 'Home',       '/',                        1, 1),
    (1, 'About Us',   '/about',                   1, 2),
    (1, 'Placements', '/placements/volunteering',  1, 3),
    (1, 'Our Impact', '/our-impact',               1, 4),
    (1, 'Locations',  '/locations',                1, 5),
    (1, 'Fees',       '/fees',                     1, 6),
    (1, 'FAQs',       '/faqs',                     1, 7),
    (1, 'News',       '/news',                     1, 8),
    (1, 'Apply Now',  '/apply',                    1, 9),
    (1, 'Contact Us', '/contact',                  1, 10);
    PRINT 'MenuItems seeded for Tenant 1.';
END
ELSE
    PRINT 'MenuItems already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- PROGRAMS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM Programs WHERE TenantId = 1)
BEGIN
    INSERT INTO Programs (TenantId, Title, Description, ImageUrl, Category, IsVisible, [Order]) VALUES
    (1, N'Teaching in Schools', N'Help children in rural Nepal get quality education.', N'https://images.unsplash.com/photo-1497486751825-1233686d5d80?w=600&q=80', N'Volunteering', 1, 1),
    (1, N'Teaching in Monastery', N'Live and teach in a Buddhist monastery.', N'https://images.unsplash.com/photo-1605640840605-14ac1855827b?w=600&q=80', N'Volunteering', 1, 2),
    (1, N'Medical Care', N'Support local health clinics and hospitals.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Volunteering', 1, 3),
    (1, N'Construction Work', N'Help rebuild schools and community centers.', N'https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=600&q=80', N'Volunteering', 1, 4),
    (1, N'Child Care', N'Work with children in orphanages and care centers.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Volunteering', 1, 5),
    (1, N'Women''s Empowerment', N'Support programs that help women gain skills and financial independence.', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=600&q=80', N'Volunteering', 1, 6),
    (1, N'Special Needs Care', N'Support disabled people who are often forced to live in isolation.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Volunteering', 1, 7),
    (1, N'Family Volunteering', N'A unique volunteering experience for families with children of all ages.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Volunteering', 1, 8),
    (1, N'Group / Educational Excursion', N'Nepal is a great destination for school and university groups.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Volunteering', 1, 9),
    (1, N'Health & Medical Internship', N'Gain hands-on clinical experience in Nepal hospitals and clinics.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Internship', 1, 10),
    (1, N'Nursing Internship', N'Develop your nursing skills in a real-world international setting.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Internship', 1, 11),
    (1, N'Pre-Medical Internship', N'Perfect for pre-med students seeking international clinical exposure.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Internship', 1, 12),
    (1, N'Public Health Internship', N'Work on community health projects and disease prevention programs.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Internship', 1, 13),
    (1, N'Physiotherapy Internship', N'Help rehabilitating children and elderly patients in Nepal.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Internship', 1, 14),
    (1, N'Nepal Experience Program', N'Combine volunteering with cultural immersion, language learning, and adventure.', N'https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80', N'Nepal Experience', 1, 15),
    (1, N'Nepali Language School', N'Learn the basics of Nepali before your volunteer journey.', N'https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80', N'Language School', 1, 16),
    (1, N'Summer Volunteer Program', N'Make the most of your summer break volunteering in Nepal.', N'https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80', N'Summer Program', 1, 17),
    (1, N'Volunteer And Yoga', N'Combine meaningful volunteering with yoga and meditation in Nepal.', N'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600&q=80', N'Volunteer + Travel', 1, 18),
    (1, N'Child Sponsorship', N'Sponsor a child in Nepal for NPR 194 a day and change their life forever.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Other', 1, 19),
    (1, N'Charity Tour & Trek', N'Get involved in charity treks and tours to support needy children and women.', N'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600&q=80', N'Other', 1, 20);
    PRINT 'Programs seeded for Tenant 1.';
END
ELSE
    PRINT 'Programs already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- TOURS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM Tours WHERE TenantId = 1)
BEGIN
    INSERT INTO Tours (TenantId, Title, Destination, Duration, Difficulty, Type, ImageUrl, Description, IsVisible, [Order]) VALUES
    (1, N'Kathmandu Pokhara Chitwan Tour', N'Nepal', N'8 Days', N'Easy', N'Tour', N'https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80', N'Explore the highlights of Nepal in one unforgettable journey.', 1, 1),
    (1, N'Annapurna Circuit Trek', N'Nepal', N'15 Days', N'Moderate', N'Trekking', N'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600&q=80', N'One of the world''s greatest treks, circling the Annapurna massif.', 1, 2),
    (1, N'Everest Base Camp Trek', N'Nepal', N'15 Days', N'Moderate', N'Trekking', N'https://images.unsplash.com/photo-1516912481808-3406841bd33c?w=600&q=80', N'Trek to the base of the world''s highest mountain.', 1, 3),
    (1, N'Chitwan Jungle Safari Tour', N'Nepal', N'6 Days', N'Easy', N'Tour', N'https://images.unsplash.com/photo-1564760055775-d63b17a55c44?w=600&q=80', N'Experience Nepal''s wildlife in Chitwan National Park.', 1, 4),
    (1, N'Annapurna Base Camp Trek', N'Nepal', N'12 Days', N'Moderate', N'Trekking', N'https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=600&q=80', N'Trek into the heart of the Annapurna Sanctuary.', 1, 5),
    (1, N'Pokhara Chitwan Tour and Volunteering', N'Nepal', N'21 Days', N'Easy', N'Tour + Volunteer', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Combine sightseeing with meaningful volunteer work.', 1, 6),
    (1, N'Everest Base Camp Trek and Volunteering', N'Nepal', N'35 Days', N'Moderate', N'Trekking + Volunteer', N'https://images.unsplash.com/photo-1516912481808-3406841bd33c?w=600&q=80', N'The ultimate Nepal experience — trek and volunteer.', 1, 7);
    PRINT 'Tours seeded for Tenant 1.';
END
ELSE
    PRINT 'Tours already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- NEWS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM News WHERE TenantId = 1)
BEGIN
    INSERT INTO News (TenantId, Title, Summary, Body, ImageUrl, Category, PublishedAt) VALUES
    (1, N'Volunteers Help Rebuild School in Sindhupalchok', N'A team of 12 international volunteers spent 3 weeks rebuilding a primary school.', N'A team of 12 international volunteers spent 3 weeks rebuilding a primary school damaged by the 2015 earthquake.', N'https://images.unsplash.com/photo-1497486751825-1233686d5d80?w=600&q=80', N'Construction', '2026-04-16 17:44:26'),
    (1, N'Medical Camp Reaches 500 Patients in Remote Village', N'Our medical volunteers organized a free health camp in a remote village of Humla.', N'Our medical volunteers organized a free health camp providing care to over 500 patients.', N'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=600&q=80', N'Medical', '2026-04-16 17:44:26'),
    (1, N'Women Empowerment Workshop Graduates 30 Women', N'Thirty women completed our 3-month skills training program.', N'Thirty women from Kathmandu valley completed our 3-month skills training program gaining financial independence.', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=600&q=80', N'Empowerment', '2026-04-16 17:44:26'),
    (1, N'New Volunteer Batch Arrives for Summer Program', N'25 new volunteers from 8 countries have arrived in Kathmandu.', N'25 new volunteers from 8 countries have arrived to begin their summer volunteer journey with Diyalo.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=600&q=80', N'Volunteering', '2026-04-16 17:44:26');
    PRINT 'News seeded for Tenant 1.';
END
ELSE
    PRINT 'News already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- TESTIMONIALS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM Testimonials WHERE TenantId = 1)
BEGIN
    INSERT INTO Testimonials (TenantId, Name, Country, Message, ImageUrl, IsVisible) VALUES
    (1, N'Chiara Chirizzi', N'Italy', N'Thanks to Diyalo I had a great experience in Nepal. They look after you and support you during your journey.', N'', 1),
    (1, N'Kelsey Brethour', N'Canada', N'The staff were so supportive and caring throughout my volunteer experience.', N'', 1),
    (1, N'Julia Michalkiewicz', N'Poland', N'I recommend Diyalo very much, especially if you go to Nepal for the first time.', N'', 1),
    (1, N'Marcus Weber', N'Germany', N'An incredible experience from start to finish. The construction project will benefit the community for generations.', N'', 1);
    PRINT 'Testimonials seeded for Tenant 1.';
END
ELSE
    PRINT 'Testimonials already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- FAQS (Tenant 1)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM Faqs WHERE TenantId = 1)
BEGIN
    INSERT INTO Faqs (TenantId, Question, Answer, [Order], IsVisible) VALUES
    (1, N'How do I apply to volunteer?', N'Fill out our online application form on the Apply Now page. Our team will review your application and get back to you within 3-5 business days.', 1, 1),
    (1, N'What is the minimum age to volunteer?', N'The minimum age is 18 years old for most programs.', 2, 1),
    (1, N'How long can I volunteer?', N'We offer flexible durations from 2 weeks to 3 months.', 3, 1),
    (1, N'What is included in the program fee?', N'The fee includes accommodation, meals, airport pickup, orientation, program placement, 24/7 local support, and a certificate of completion.', 4, 1),
    (1, N'Do I need to speak Nepali?', N'No, English is widely spoken in our programs.', 5, 1);
    PRINT 'FAQs seeded for Tenant 1.';
END
ELSE
    PRINT 'FAQs already exist for Tenant 1 — skipped.';
GO

-- =============================================================================
-- HERO SLIDES (Tenant 1)
-- =============================================================================
UPDATE HeroSlides SET Badge=N'Volunteer in Nepal', Title=N'Light the Way.', Highlight=N'Change a Life.', Subtitle=N'Diyalo connects passionate volunteers with communities that need them most.', IsVisible=1 WHERE [Order]=1 AND TenantId=1;
UPDATE HeroSlides SET Badge=N'Make an Impact', Title=N'Help Rebuild', Highlight=N'Nepal.', Subtitle=N'Join our construction and community development programs across Nepal.', IsVisible=1 WHERE [Order]=2 AND TenantId=1;
UPDATE HeroSlides SET Badge=N'Teach & Inspire', Title=N'Educate a Child.', Highlight=N'Shape the Future.', Subtitle=N'Volunteer as a teacher and give children the gift of education.', IsVisible=1 WHERE [Order]=3 AND TenantId=1;
PRINT 'HeroSlides updated for Tenant 1.';
GO

-- =============================================================================
-- SITE SETTINGS (Tenant 1)
-- =============================================================================
UPDATE SiteSettings SET Value=N'Diyalo'               WHERE [Key]=N'siteName'        AND TenantId=1;
UPDATE SiteSettings SET Value=N'Kathmandu, Nepal'     WHERE [Key]=N'address'         AND TenantId=1;
UPDATE SiteSettings SET Value=N'+977 9800000000'      WHERE [Key]=N'phone'           AND TenantId=1;
UPDATE SiteSettings SET Value=N'contact@diyalo.org'   WHERE [Key]=N'email'           AND TenantId=1;
UPDATE SiteSettings SET Value=N'Sun - Fri: 9am - 5pm' WHERE [Key]=N'officeHours'    AND TenantId=1;
UPDATE SiteSettings SET Value=N'#e63946' WHERE [Key]=N'primaryColor'   AND TenantId=1;
UPDATE SiteSettings SET Value=N'#457b9d' WHERE [Key]=N'secondaryColor' AND TenantId=1;
UPDATE SiteSettings SET Value=N'#ffffff' WHERE [Key]=N'navbarColor'    AND TenantId=1;
UPDATE SiteSettings SET Value=N'#1d3557' WHERE [Key]=N'footerColor'    AND TenantId=1;
UPDATE SiteSettings SET Value=N'#e63946' WHERE [Key]=N'buttonColor'    AND TenantId=1;

IF NOT EXISTS (SELECT 1 FROM SiteSettings WHERE [Key]=N'videoUrl' AND TenantId=1)
    INSERT INTO SiteSettings (TenantId,[Key],Value) VALUES (1,'videoUrl','https://www.youtube.com/embed/dQw4w9WgXcQ');
IF NOT EXISTS (SELECT 1 FROM SiteSettings WHERE [Key]=N'videoTitle' AND TenantId=1)
    INSERT INTO SiteSettings (TenantId,[Key],Value) VALUES (1,'videoTitle','Watch This Video To Know How Exciting Our Programs Are!');
IF NOT EXISTS (SELECT 1 FROM SiteSettings WHERE [Key]=N'videoSubtitle' AND TenantId=1)
    INSERT INTO SiteSettings (TenantId,[Key],Value) VALUES (1,'videoSubtitle','A glimpse of the volunteering journey in Nepal');
PRINT 'SiteSettings updated for Tenant 1.';
GO

PRINT '=== Seed complete ===';
