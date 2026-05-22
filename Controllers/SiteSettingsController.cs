using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class SiteSettingsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public SiteSettingsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(new Dictionary<string, string>());
        var settings = await _db.SiteSettings.Where(s => s.TenantId == tid).ToListAsync();
        return Ok(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateAll([FromBody] Dictionary<string, string> updates)
    {
        var tid = await GetTenantIdAsync();
        foreach (var kv in updates)
        {
            var setting = await _db.SiteSettings
                .FirstOrDefaultAsync(s => s.TenantId == tid && s.Key == kv.Key);
            if (setting != null)
                setting.Value = kv.Value;
            else
                _db.SiteSettings.Add(new SiteSetting { TenantId = tid, Key = kv.Key, Value = kv.Value });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Settings saved" });
    }
}
