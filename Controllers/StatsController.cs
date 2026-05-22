using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class StatsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public StatsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(new { programs=0, news=0, applications=0, testimonials=0, tours=0, volunteers="500+", communities="20+", livesImpacted="1000+", yearsActive="10+" });

        var programCount     = await _db.Programs.CountAsync(p => p.TenantId == tid && p.IsVisible);
        var newsCount        = await _db.News.CountAsync(n => n.TenantId == tid);
        var applicationCount = await _db.Applications.CountAsync(a => a.TenantId == tid);
        var testimonialCount = await _db.Testimonials.CountAsync(t => t.TenantId == tid && t.IsVisible);
        var tourCount        = await _db.Tours.CountAsync(t => t.TenantId == tid && t.IsVisible);
        var settings         = await _db.SiteSettings.Where(s => s.TenantId == tid).ToListAsync();
        string Get(string key, string fallback) => settings.FirstOrDefault(s => s.Key == key)?.Value ?? fallback;

        return Ok(new
        {
            programs=programCount, news=newsCount, applications=applicationCount,
            testimonials=testimonialCount, tours=tourCount,
            volunteers=Get("stat_volunteers","500+"), communities=Get("stat_communities","20+"),
            livesImpacted=Get("stat_livesImpacted","1000+"), yearsActive=Get("stat_yearsActive","10+"),
        });
    }
}
