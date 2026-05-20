using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class MenuItemsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public MenuItemsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdAsync();
        var all = await _db.MenuItems
            .Where(m => m.TenantId == tid)
            .OrderBy(m => m.Order)
            .ToListAsync();

        // Return top-level visible items, each with their visible children embedded
        var result = all
            .Where(m => m.ParentId == null && m.IsVisible)
            .Select(parent => new
            {
                parent.Id,
                parent.Label,
                parent.Url,
                parent.IsVisible,
                parent.Order,
                parent.ParentId,
                subItems = all
                    .Where(c => c.ParentId == parent.Id && c.IsVisible)
                    .OrderBy(c => c.Order)
                    .Select(c => new { c.Id, c.Label, c.Url, c.IsVisible, c.Order, c.ParentId })
                    .ToList()
            });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        var all = await _db.MenuItems
            .Where(m => m.TenantId == tid)
            .OrderBy(m => m.Order)
            .ToListAsync();

        // Return top-level items with their children nested (same shape as GetVisible)
        var result = all
            .Where(m => m.ParentId == null)
            .Select(parent => new
            {
                parent.Id,
                parent.Label,
                parent.Url,
                parent.IsVisible,
                parent.Order,
                parent.ParentId,
                subItems = all
                    .Where(c => c.ParentId == parent.Id)
                    .OrderBy(c => c.Order)
                    .Select(c => new { c.Id, c.Label, c.Url, c.IsVisible, c.Order, c.ParentId })
                    .ToList()
            });

        return Ok(result);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MenuItem updated)
    {
        var tid  = await GetTenantIdAsync();
        var item = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tid);
        if (item == null) return NotFound();
        item.Label     = updated.Label;
        item.Url       = updated.Url;
        item.IsVisible = updated.IsVisible;
        item.Order     = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
