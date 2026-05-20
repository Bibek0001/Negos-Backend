using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

/// <summary>
/// Super Admin only — manage all tenants on the platform.
/// All endpoints require the "SuperAdmin" role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantProvisioningService _provisioning;

    public TenantsController(AppDbContext db, TenantProvisioningService provisioning)
    {
        _db           = db;
        _provisioning = provisioning;
    }

    // GET /api/tenants — list all tenants
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Tenants.OrderBy(t => t.CreatedAt).ToListAsync());

    // GET /api/tenants/{id} — get single tenant with stats
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        var stats = new
        {
            programs     = await _db.Programs.CountAsync(p => p.TenantId == id),
            news         = await _db.News.CountAsync(n => n.TenantId == id),
            tours        = await _db.Tours.CountAsync(t => t.TenantId == id),
            applications = await _db.Applications.CountAsync(a => a.TenantId == id),
            testimonials = await _db.Testimonials.CountAsync(t => t.TenantId == id),
        };

        return Ok(new { tenant, stats });
    }

    // POST /api/tenants — create new tenant + provision default data
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req)
    {
        // Validate subdomain is unique
        if (await _db.Tenants.AnyAsync(t => t.Subdomain == req.Subdomain))
            return BadRequest(new { message = $"Subdomain '{req.Subdomain}' is already taken." });

        var tenant = new Tenant
        {
            Subdomain = req.Subdomain.ToLower().Trim(),
            Name      = req.Name.Trim(),
            Email     = req.Email.Trim(),
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(); // save to get the Id

        // Provision default data for this tenant
        await _provisioning.ProvisionAsync(tenant);

        // Create a default admin user for this tenant
        // Default password: Admin@123 (tenant admin should change this)
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        _db.AdminUsers.Add(new AdminUser
        {
            TenantId     = tenant.Id,
            Username     = req.AdminUsername ?? $"admin_{req.Subdomain}",
            PasswordHash = adminHash,
            Role         = "Admin",
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Tenant '{tenant.Name}' created successfully.",
            tenant,
            adminUsername = req.AdminUsername ?? $"admin_{req.Subdomain}",
            adminPassword = "Admin@123",
            url           = $"https://{tenant.Subdomain}.negos.org",
        });
    }

    // PUT /api/tenants/{id} — update tenant info
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantRequest req)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        // Check subdomain uniqueness if changing it
        if (req.Subdomain != tenant.Subdomain &&
            await _db.Tenants.AnyAsync(t => t.Subdomain == req.Subdomain && t.Id != id))
            return BadRequest(new { message = $"Subdomain '{req.Subdomain}' is already taken." });

        tenant.Name      = req.Name.Trim();
        tenant.Subdomain = req.Subdomain.ToLower().Trim();
        tenant.Email     = req.Email.Trim();
        await _db.SaveChangesAsync();

        return Ok(tenant);
    }

    // PUT /api/tenants/{id}/toggle — activate or deactivate tenant
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        tenant.IsActive = !tenant.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Tenant '{tenant.Name}' is now {(tenant.IsActive ? "active" : "inactive")}.", tenant });
    }

    // DELETE /api/tenants/{id} — permanently delete tenant and all their data
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        // Remove all tenant data
        _db.Programs.RemoveRange(_db.Programs.Where(p => p.TenantId == id));
        _db.News.RemoveRange(_db.News.Where(n => n.TenantId == id));
        _db.Tours.RemoveRange(_db.Tours.Where(t => t.TenantId == id));
        _db.Testimonials.RemoveRange(_db.Testimonials.Where(t => t.TenantId == id));
        _db.Faqs.RemoveRange(_db.Faqs.Where(f => f.TenantId == id));
        _db.HeroSlides.RemoveRange(_db.HeroSlides.Where(h => h.TenantId == id));
        _db.MenuItems.RemoveRange(_db.MenuItems.Where(m => m.TenantId == id));
        _db.SiteSettings.RemoveRange(_db.SiteSettings.Where(s => s.TenantId == id));
        _db.Applications.RemoveRange(_db.Applications.Where(a => a.TenantId == id));
        _db.ContactMessages.RemoveRange(_db.ContactMessages.Where(c => c.TenantId == id));
        _db.AdminUsers.RemoveRange(_db.AdminUsers.Where(u => u.TenantId == id));
        _db.Tenants.Remove(tenant);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Tenant '{tenant.Name}' and all their data have been deleted." });
    }

    // -----------------------------------------------------------------------
    // Admin User Management
    // -----------------------------------------------------------------------

    // GET /api/tenants/{id}/admins — list all admin users for a tenant
    [HttpGet("{id}/admins")]
    public async Task<IActionResult> GetAdmins(int id)
    {
        var admins = await _db.AdminUsers
            .Where(u => u.TenantId == id)
            .Select(u => new { u.Id, u.Username, u.Role, u.TenantId })
            .ToListAsync();
        return Ok(admins);
    }

    // POST /api/tenants/{id}/admins — create a new admin for a tenant
    [HttpPost("{id}/admins")]
    public async Task<IActionResult> CreateAdmin(int id, [FromBody] CreateAdminRequest req)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        if (await _db.AdminUsers.AnyAsync(u => u.Username == req.Username))
            return BadRequest(new { message = $"Username '{req.Username}' is already taken." });

        var admin = new AdminUser
        {
            TenantId     = id,
            Username     = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = "Admin",
        };
        _db.AdminUsers.Add(admin);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Admin created.", admin = new { admin.Id, admin.Username, admin.Role } });
    }

    // PUT /api/tenants/{tenantId}/admins/{adminId}/reset-password — reset a tenant admin's password
    [HttpPut("{tenantId}/admins/{adminId}/reset-password")]
    public async Task<IActionResult> ResetPassword(int tenantId, int adminId, [FromBody] ResetPasswordRequest req)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminId && u.TenantId == tenantId);
        if (admin == null) return NotFound();
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Password reset for '{admin.Username}'." });
    }

    // DELETE /api/tenants/{tenantId}/admins/{adminId} — delete a tenant admin
    [HttpDelete("{tenantId}/admins/{adminId}")]
    public async Task<IActionResult> DeleteAdmin(int tenantId, int adminId)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminId && u.TenantId == tenantId);
        if (admin == null) return NotFound();
        _db.AdminUsers.Remove(admin);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Admin '{admin.Username}' deleted." });
    }

    // -----------------------------------------------------------------------
    // Impersonation — Super Admin logs in as a tenant admin
    // -----------------------------------------------------------------------

    // POST /api/tenants/{id}/impersonate — get a short-lived token for a tenant
    [HttpPost("{id}/impersonate")]
    public async Task<IActionResult> Impersonate(int id, [FromServices] AuthService auth)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        // Find the first admin for this tenant
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(u => u.TenantId == id && u.Role == "Admin");
        if (admin == null)
            return BadRequest(new { message = "No admin user found for this tenant." });

        var token = auth.GenerateToken(admin);
        return Ok(new { token, username = admin.Username, tenantName = tenant.Name });
    }

    // -----------------------------------------------------------------------
    // Tenant Content Overview (read-only for super admin)
    // -----------------------------------------------------------------------

    // GET /api/tenants/{id}/sitesettings — get all site settings for a tenant
    [HttpGet("{id}/sitesettings")]
    public async Task<IActionResult> GetSiteSettings(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        var settings = await _db.SiteSettings.Where(s => s.TenantId == id).ToListAsync();
        return Ok(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    // PUT /api/tenants/{id}/sitesettings — update site settings for a tenant
    [HttpPut("{id}/sitesettings")]
    public async Task<IActionResult> UpdateSiteSettings(int id, [FromBody] Dictionary<string, string> updates)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        foreach (var kv in updates)
        {
            var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.TenantId == id && s.Key == kv.Key);
            if (setting != null) setting.Value = kv.Value;
            else _db.SiteSettings.Add(new SiteSetting { TenantId = id, Key = kv.Key, Value = kv.Value });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Settings updated." });
    }

    // GET /api/tenants/{id}/menuitems — get all menu items for a tenant
    [HttpGet("{id}/menuitems")]
    public async Task<IActionResult> GetMenuItems(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        var items = await _db.MenuItems.Where(m => m.TenantId == id).OrderBy(m => m.Order).ToListAsync();
        return Ok(items);
    }

    // PUT /api/tenants/{id}/menuitems/{itemId} — toggle a menu item for a tenant
    [HttpPut("{id}/menuitems/{itemId}")]
    public async Task<IActionResult> UpdateMenuItem(int id, int itemId, [FromBody] MenuItem updated)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == itemId && m.TenantId == id);
        if (item == null) return NotFound();
        item.Label     = updated.Label;
        item.Url       = updated.Url;
        item.IsVisible = updated.IsVisible;
        item.Order     = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // GET /api/tenants/{id}/content — get all content counts + recent items
    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        return Ok(new
        {
            programs     = await _db.Programs.Where(p => p.TenantId == id).OrderBy(p => p.Order).Select(p => new { p.Id, p.Title, p.Category, p.IsVisible }).ToListAsync(),
            news         = await _db.News.Where(n => n.TenantId == id).OrderByDescending(n => n.PublishedAt).Select(n => new { n.Id, n.Title, n.Category, n.PublishedAt }).ToListAsync(),
            tours        = await _db.Tours.Where(t => t.TenantId == id).OrderBy(t => t.Order).Select(t => new { t.Id, t.Title, t.Type, t.IsVisible }).ToListAsync(),
            applications = await _db.Applications.Where(a => a.TenantId == id).OrderByDescending(a => a.SubmittedAt).Select(a => new { a.Id, a.Name, a.Program, a.Status, a.SubmittedAt }).Take(10).ToListAsync(),
            messages     = await _db.ContactMessages.Where(m => m.TenantId == id).OrderByDescending(m => m.SentAt).Select(m => new { m.Id, m.Name, m.Subject, m.IsRead, m.SentAt }).Take(10).ToListAsync(),
        });
    }
}

// ---------------------------------------------------------------------------
// Request DTOs
// ---------------------------------------------------------------------------
public record CreateTenantRequest(
    string Name,
    string Subdomain,
    string Email,
    string? AdminUsername
);

public record UpdateTenantRequest(
    string Name,
    string Subdomain,
    string Email
);

public record CreateAdminRequest(string Username, string Password);
public record ResetPasswordRequest(string NewPassword);
