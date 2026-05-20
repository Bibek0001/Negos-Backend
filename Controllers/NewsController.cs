using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class NewsController : TenantBaseController
{
    private readonly AppDbContext _db;
    public NewsController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.News
            .Where(n => n.TenantId == tid)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tid = await GetTenantIdAsync();
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tid);
        return news == null ? NotFound() : Ok(news);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] News news)
    {
        news.TenantId    = await GetTenantIdAsync();
        news.PublishedAt = DateTime.UtcNow;
        _db.News.Add(news);
        await _db.SaveChangesAsync();
        return Ok(news);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] News updated)
    {
        var tid  = await GetTenantIdAsync();
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tid);
        if (news == null) return NotFound();
        news.Title    = updated.Title;
        news.Summary  = updated.Summary;
        news.Body     = updated.Body;
        news.ImageUrl = updated.ImageUrl;
        news.Category = updated.Category;
        await _db.SaveChangesAsync();
        return Ok(news);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid  = await GetTenantIdAsync();
        var news = await _db.News.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tid);
        if (news == null) return NotFound();
        _db.News.Remove(news);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
