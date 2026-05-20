using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class TestimonialsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public TestimonialsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Testimonials
            .Where(t => t.TenantId == tid && t.IsVisible)
            .ToListAsync());
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Testimonials
            .Where(t => t.TenantId == tid)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Testimonial t)
    {
        t.TenantId = await GetTenantIdAsync();
        _db.Testimonials.Add(t);
        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Testimonial updated)
    {
        var tid = await GetTenantIdAsync();
        var t   = await _db.Testimonials.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid);
        if (t == null) return NotFound();
        t.Name      = updated.Name;
        t.Country   = updated.Country;
        t.Message   = updated.Message;
        t.ImageUrl  = updated.ImageUrl;
        t.IsVisible = updated.IsVisible;
        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid = await GetTenantIdAsync();
        var t   = await _db.Testimonials.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid);
        if (t == null) return NotFound();
        _db.Testimonials.Remove(t);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
