using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class ProgramsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public ProgramsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(Array.Empty<object>());
        return Ok(await _db.Programs
            .Where(p => p.TenantId == tid && p.IsVisible)
            .OrderBy(p => p.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Programs
            .Where(p => p.TenantId == tid)
            .OrderBy(p => p.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VolunteerProgram program)
    {
        program.TenantId = await GetTenantIdAsync();
        _db.Programs.Add(program);
        await _db.SaveChangesAsync();
        return Ok(program);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] VolunteerProgram updated)
    {
        var tid = await GetTenantIdAsync();
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid);
        if (program == null) return NotFound();
        program.Title       = updated.Title;
        program.Description = updated.Description;
        program.ImageUrl    = updated.ImageUrl;
        program.Category    = updated.Category;
        program.IsVisible   = updated.IsVisible;
        program.Order       = updated.Order;
        await _db.SaveChangesAsync();
        return Ok(program);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid = await GetTenantIdAsync();
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid);
        if (program == null) return NotFound();
        _db.Programs.Remove(program);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
