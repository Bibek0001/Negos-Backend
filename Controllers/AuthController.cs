using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;
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

    /// <summary>
    /// Login for both tenant admins and super admins.
    /// Response includes role and tenantId so the frontend can route correctly:
    ///   role = "SuperAdmin" → redirect to /superadmin/dashboard
    ///   role = "Admin"      → redirect to /admin/dashboard
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
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

    [Authorize]
    [HttpPost("change-password")]
    public IActionResult ChangePassword()
    {
        return StatusCode(403, new { message = "Admin credentials are fixed and cannot be changed." });
    }
}

public record LoginRequest(string Username, string Password);
