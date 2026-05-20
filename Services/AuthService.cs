using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Diyalo.Api.Models;

namespace Diyalo.Api.Services;

public class AuthService
{
    private readonly IConfiguration _config;
    public AuthService(IConfiguration config) => _config = config;

    public string GenerateToken(AdminUser user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("tenantId",                user.TenantId.ToString()),
            new Claim(ClaimTypes.Role,           user.Role),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
