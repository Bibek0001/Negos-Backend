using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class ToursController : TenantBaseController
{
    private readonly AppDbContext _db;
    public ToursController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(Array.Empty<object>());
        return Ok(await _db.Tours
            .Where(t => t.TenantId == tid && t.IsVisible)
            .OrderBy(t => t.Order)
            .ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tid  = await GetTenantIdSafeAsync();
        if (tid < 0) return NotFound();
        var tour = await _db.Tours.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tid);
        return tour == null ? NotFound() : Ok(tour);
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Tours
            .Where(t => t.TenantId == tid)
            .OrderBy(t => t.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Tour tour)
    {
        tour.TenantId = await GetTenantIdAsync();
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();
        return Ok(tour);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Tour updated)
    {
        var tid  = await GetTenantIdAsync();
        var tour = await _db.Tours.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tid);
        if (tour == null) return NotFound();
        tour.Title       = updated.Title;
        tour.Destination = updated.Destination;
        tour.Duration    = updated.Duration;
        tour.Difficulty  = updated.Difficulty;
        tour.Type        = updated.Type;
        tour.Description = updated.Description;
        tour.ImageUrl    = updated.ImageUrl;
        tour.IsVisible   = updated.IsVisible;
        tour.Order       = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(tour);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid  = await GetTenantIdAsync();
        var tour = await _db.Tours.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tid);
        if (tour == null) return NotFound();
        _db.Tours.Remove(tour);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
