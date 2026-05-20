using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[Route("api/[controller]")]
public class ApplicationsController : TenantBaseController
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public ApplicationsController(AppDbContext db, EmailService email, TenantService ts) : base(ts)
    {
        _db    = db;
        _email = email;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] Application application)
    {
        application.TenantId    = await GetTenantIdAsync();
        application.SubmittedAt = DateTime.UtcNow;
        _db.Applications.Add(application);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Application submitted successfully" });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tid = await GetTenantIdAsync();
        return Ok(await _db.Applications
            .Where(a => a.TenantId == tid)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync());
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tid = await GetTenantIdAsync();
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tid);
        if (app == null) return NotFound();
        _db.Applications.Remove(app);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusRequest req)
    {
        var tid = await GetTenantIdAsync();
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tid);
        if (app == null) return NotFound();
        app.Status = req.Status;
        await _db.SaveChangesAsync();

        try
        {
            string subject, body;
            if (req.Status == "Approved")
            {
                subject = "Your Volunteer Application has been Approved!";
                body = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
                    <div style='background:#1d3557;padding:20px;border-radius:8px 8px 0 0;text-align:center;'>
                        <h1 style='color:#fff;margin:0;font-size:24px;'>🎉 Application Approved!</h1>
                    </div>
                    <div style='background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;'>
                        <p style='font-size:16px;color:#333;'>Dear <strong>{app.Name}</strong>,</p>
                        <p style='color:#555;'>Your application for <strong>{app.Program}</strong> has been <strong style='color:green;'>APPROVED</strong>. Our team will contact you shortly.</p>
                    </div></div>";
            }
            else
            {
                subject = "Update on Your Volunteer Application";
                body = $@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
                    <div style='background:#1d3557;padding:20px;border-radius:8px 8px 0 0;text-align:center;'>
                        <h1 style='color:#fff;margin:0;font-size:24px;'>Application Update</h1>
                    </div>
                    <div style='background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;'>
                        <p style='font-size:16px;color:#333;'>Dear <strong>{app.Name}</strong>,</p>
                        <p style='color:#555;'>We are unable to accept your application for <strong>{app.Program}</strong> at this time. We encourage you to apply again in the future.</p>
                    </div></div>";
            }
            await _email.SendAsync(app.Email, app.Name, subject, body);
        }
        catch (Exception ex) { /* Email send failed — status already saved */ _ = ex; }

        return Ok(app);
    }
}

public record StatusRequest(string Status);
