using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class ContactController : TenantBaseController
{
    private readonly AppDbContext _db;
    public ContactController(AppDbContext db, TenantService ts) : base(ts) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactMessage msg)
    {
        msg.TenantId = await GetTenantIdAsync();
        msg.SentAt   = DateTime.UtcNow;
        msg.IsRead   = false;
        _db.ContactMessages.Add(msg);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Message sent successfully" });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.ContactMessages
            .Where(m => m.TenantId == tid)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync());
    }

    [Authorize]
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var tid = await GetTenantIdAsync();
        var msg = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tid);
        if (msg == null) return NotFound();
        msg.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid = await GetTenantIdAsync();
        var msg = await _db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tid);
        if (msg == null) return NotFound();
        _db.ContactMessages.Remove(msg);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
