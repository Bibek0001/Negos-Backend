using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;

namespace Diyalo.Api.Controllers;

/// <summary>
/// Platform-level settings — only Super Admin can read/write.
/// These are stored as SiteSettings with TenantId = 0 (platform level).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlatformController : ControllerBase
{
    private readonly AppDbContext _db;
    public PlatformController(AppDbContext db) => _db = db;

    // PUBLIC — anyone can read platform name/logo (used by public landing page)
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _db.SiteSettings
            .Where(s => s.TenantId == 0)
            .ToListAsync();
        return Ok(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    // SUPER ADMIN ONLY — update platform settings
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Dictionary<string, string> updates)
    {
        foreach (var kv in updates)
        {
            var setting = await _db.SiteSettings
                .FirstOrDefaultAsync(s => s.TenantId == 0 && s.Key == kv.Key);
            if (setting != null)
                setting.Value = kv.Value;
            else
                _db.SiteSettings.Add(new SiteSetting { TenantId = 0, Key = kv.Key, Value = kv.Value });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Platform settings saved." });
    }

    // PUBLIC — get platform menu items (TenantId=0)
    [HttpGet("menuitems")]
    public async Task<IActionResult> GetMenuItems()
    {
        var all = await _db.MenuItems
            .Where(m => m.TenantId == 0)
            .OrderBy(m => m.Order)
            .ToListAsync();

        // Return top-level visible items with their visible children embedded
        var result = all
            .Where(m => m.ParentId == null && m.IsVisible)
            .Select(parent => new
            {
                parent.Id, parent.Label, parent.Url, parent.IsVisible, parent.Order, parent.ParentId,
                subItems = all
                    .Where(c => c.ParentId == parent.Id && c.IsVisible)
                    .OrderBy(c => c.Order)
                    .Select(c => new { c.Id, c.Label, c.Url, c.IsVisible, c.Order, c.ParentId })
                    .ToList()
            });

        return Ok(result);
    }

    // SUPER ADMIN ONLY — get ALL platform menu items (including hidden), nested
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("menuitems/all")]
    public async Task<IActionResult> GetAllMenuItems()
    {
        var all = await _db.MenuItems
            .Where(m => m.TenantId == 0)
            .OrderBy(m => m.Order)
            .ToListAsync();

        // Return nested structure (same shape as public endpoint but includes hidden items)
        var result = all
            .Where(m => m.ParentId == null)
            .Select(parent => new
            {
                parent.Id, parent.Label, parent.Url, parent.IsVisible, parent.Order, parent.ParentId,
                subItems = all
                    .Where(c => c.ParentId == parent.Id)
                    .OrderBy(c => c.Order)
                    .Select(c => new { c.Id, c.Label, c.Url, c.IsVisible, c.Order, c.ParentId })
                    .ToList()
            });

        return Ok(result);
    }

    // SUPER ADMIN ONLY — update a platform menu item
    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("menuitems/{id}")]
    public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] MenuItem updated)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == 0);
        if (item == null) return NotFound();
        item.Label     = updated.Label;
        item.Url       = updated.Url;
        item.IsVisible = updated.IsVisible;
        item.Order     = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
