using System.Security.Claims;
using Diyalo.Api.Data;
using Diyalo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Diyalo.Api.Services;

/// <summary>
/// Resolves the current tenant from the HTTP request.
///
/// Priority order:
///   1. JWT token "tenantId" claim (for authenticated admin requests)
///      — SuperAdmin (TenantId=0) falls through to subdomain resolution
///   2. Subdomain from Host header (for public requests)
///   3. First active tenant (localhost / dev fallback)
/// </summary>
public class TenantService
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext         _db;
    private Tenant? _cached;

    public TenantService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db   = db;
    }

    public async Task<Tenant> GetCurrentTenantAsync()
    {
        if (_cached != null) return _cached;

        var ctx = _http.HttpContext;

        // -----------------------------------------------------------------------
        // 1. Authenticated request — use tenantId from JWT claim
        //    (SuperAdmin has TenantId=0, so they fall through to subdomain)
        // -----------------------------------------------------------------------
        if (ctx?.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = ctx.User.FindFirst("tenantId")?.Value;
            if (int.TryParse(tenantClaim, out var claimTenantId) && claimTenantId > 0)
            {
                var tenantFromClaim = await _db.Tenants
                    .FirstOrDefaultAsync(t => t.Id == claimTenantId && t.IsActive);
                if (tenantFromClaim != null)
                {
                    _cached = tenantFromClaim;
                    return _cached;
                }
            }
        }

        // -----------------------------------------------------------------------
        // 2. Public request — resolve from subdomain
        // -----------------------------------------------------------------------
        var host      = ctx?.Request.Host.Host ?? string.Empty;
        var subdomain = ExtractSubdomain(host);

        Tenant? tenant;

        if (!string.IsNullOrEmpty(subdomain))
        {
            tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);
        }
        else
        {
            // Dev / localhost — use first active tenant
            tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.IsActive);
        }

        if (tenant == null)
            throw new InvalidOperationException($"Tenant not found for host: {host}");

        _cached = tenant;
        return tenant;
    }

    public int GetTenantId() => _cached?.Id
        ?? throw new InvalidOperationException("Tenant not resolved yet.");

    private static string ExtractSubdomain(string host)
    {
        var hostOnly = host.Split(':')[0];

        // Treat these as no-subdomain hosts — fall back to first active tenant
        if (hostOnly == "localhost" ||
            System.Net.IPAddress.TryParse(hostOnly, out _) ||
            hostOnly.EndsWith(".onrender.com") ||
            hostOnly.EndsWith(".netlify.app") ||
            hostOnly.EndsWith(".fly.dev") ||
            hostOnly.EndsWith(".railway.app") ||
            hostOnly.EndsWith(".up.railway.app"))
            return string.Empty;

        var parts = hostOnly.Split('.');
        return parts.Length >= 3 ? parts[0] : string.Empty;
    }
}
