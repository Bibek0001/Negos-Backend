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

    // ---------------------------------------------------------------------------
    // Hardcoded fallback credentials — work even if the database is unavailable.
    // These are always checked first before hitting the DB.
    // ---------------------------------------------------------------------------
    private static readonly (string Username, string Password, string Role, int TenantId)[] _hardcoded = new[]
    {
        ("Negos",           "Negos@123",      "SuperAdmin", 0),
        ("NegosBk",         "NegosBk@2026",   "SuperAdmin", 0),
        ("admin_diyalo",    "Diyalo@123",      "Admin",      1),
        ("admin_volunteer", "Volunteer@123",   "Admin",      2),
        ("admin_nepalhelp", "NepalHelp@123",   "Admin",      3),
    };

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db   = db;
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // -----------------------------------------------------------------------
        // 1. Check hardcoded credentials first — works without DB
        // -----------------------------------------------------------------------
        var hardcoded = _hardcoded.FirstOrDefault(h =>
            h.Username == req.Username && h.Password == req.Password);

        if (hardcoded != default)
        {
            var hardcodedUser = new AdminUser
            {
                Id           = hardcoded.TenantId == 0 ? 9999 : hardcoded.TenantId + 100,
                Username     = hardcoded.Username,
                PasswordHash = string.Empty,
                Role         = hardcoded.Role,
                TenantId     = hardcoded.TenantId,
            };
            return Ok(new
            {
                token    = _auth.GenerateToken(hardcodedUser),
                role     = hardcodedUser.Role,
                tenantId = hardcodedUser.TenantId,
                username = hardcodedUser.Username,
            });
        }

        // -----------------------------------------------------------------------
        // 2. Fall back to database lookup
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
            // DB unavailable — hardcoded check already failed above
            return Unauthorized(new { message = "Invalid username or password" });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public IActionResult ChangePassword()
    {
        return StatusCode(403, new { message = "Admin credentials are fixed and cannot be changed." });
    }
}

public record LoginRequest(string Username, string Password);
