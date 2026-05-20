using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Models;

namespace Diyalo.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<VolunteerProgram> Programs => Set<VolunteerProgram>();
    public DbSet<News> News => Set<News>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------------------------------------------------------------------------
        // Tenant — unique subdomain
        // ---------------------------------------------------------------------------
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Subdomain)
            .IsUnique();

        // ---------------------------------------------------------------------------
        // TenantId indexes on every tenant-scoped table
        // These make all filtered queries fast even with millions of rows
        // ---------------------------------------------------------------------------
        modelBuilder.Entity<VolunteerProgram>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<News>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<Tour>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<Testimonial>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<Faq>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<HeroSlide>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<MenuItem>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<SiteSetting>()
            .HasIndex(e => new { e.TenantId, e.Key })
            .IsUnique(); // one value per key per tenant
        modelBuilder.Entity<Application>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ContactMessage>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<AdminUser>().HasIndex(e => e.TenantId);
    }
}
