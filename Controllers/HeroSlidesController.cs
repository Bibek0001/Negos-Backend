using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class HeroSlidesController : TenantBaseController
{
    private readonly AppDbContext _db;
    public HeroSlidesController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(Array.Empty<object>());
        return Ok(await _db.HeroSlides
            .Where(s => s.TenantId == tid && s.IsVisible)
            .OrderBy(s => s.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.HeroSlides
            .Where(s => s.TenantId == tid)
            .OrderBy(s => s.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HeroSlide slide)
    {
        slide.TenantId = await GetTenantIdAsync();
        _db.HeroSlides.Add(slide);
        await _db.SaveChangesAsync();
        return Ok(slide);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] HeroSlide updated)
    {
        var tid   = await GetTenantIdAsync();
        var slide = await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tid);
        if (slide == null) return NotFound();
        slide.Badge     = updated.Badge;
        slide.Title     = updated.Title;
        slide.Highlight = updated.Highlight;
        slide.Subtitle  = updated.Subtitle;
        slide.ImageUrl  = updated.ImageUrl;
        slide.IsVisible = updated.IsVisible;
        slide.Order     = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(slide);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid   = await GetTenantIdAsync();
        var slide = await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tid);
        if (slide == null) return NotFound();
        _db.HeroSlides.Remove(slide);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
