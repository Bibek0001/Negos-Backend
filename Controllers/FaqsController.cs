using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class FaqsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public FaqsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetVisible()
    {
        var tid = await GetTenantIdSafeAsync();
        if (tid < 0) return Ok(Array.Empty<object>());
        return Ok(await _db.Faqs
            .Where(f => f.TenantId == tid && f.IsVisible)
            .OrderBy(f => f.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Faqs
            .Where(f => f.TenantId == tid)
            .OrderBy(f => f.Order)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Faq faq)
    {
        faq.TenantId = await GetTenantIdAsync();
        _db.Faqs.Add(faq);
        await _db.SaveChangesAsync();
        return Ok(faq);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Faq updated)
    {
        var tid = await GetTenantIdAsync();
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tid);
        if (faq == null) return NotFound();
        faq.Question  = updated.Question;
        faq.Answer    = updated.Answer;
        faq.Order     = updated.Order;
        faq.IsVisible = updated.IsVisible;
        await _db.SaveChangesAsync();
        return Ok(faq);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid = await GetTenantIdAsync();
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tid);
        if (faq == null) return NotFound();
        _db.Faqs.Remove(faq);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
