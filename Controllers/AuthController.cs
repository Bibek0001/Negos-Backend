using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService  _auth;

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db   = db;
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // -----------------------------------------------------------------------
        // Database lookup — BCrypt-hashed passwords only
        // -----------------------------------------------------------------------
        try
        {
            var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password" });

            // For tenant admins, verify their tenant is still active
            if (user.Role == "Admin" && user.TenantId > 0)
            {
                var tenant = await _db.Tenants.FindAsync(user.TenantId);
                if (tenant == null || !tenant.IsActive)
                    return Unauthorized(new { message = "Your organization account is inactive. Contact support." });
            }

            return Ok(new
            {
                token    = _auth.GenerateToken(user),
                role     = user.Role,
                tenantId = user.TenantId,
                username = user.Username,
            });
        }
        catch
        {
            return StatusCode(503, new { message = "Service temporarily unavailable. Please try again." });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var usernameClaim = User.Identity?.Name;
        if (string.IsNullOrEmpty(usernameClaim))
            return Unauthorized();

        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == usernameClaim);
        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password changed successfully." });
    }
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
