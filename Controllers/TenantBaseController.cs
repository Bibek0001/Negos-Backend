using Microsoft.AspNetCore.Mvc;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[ApiController]
public abstract class TenantBaseController : ControllerBase
{
    protected readonly TenantService _tenantService;

    protected TenantBaseController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    protected async Task<int> GetTenantIdAsync()
    {
        var tenant = await _tenantService.GetCurrentTenantAsync();
        return tenant.Id;
    }

    /// <summary>
    /// Safe version — returns -1 instead of throwing if tenant not found.
    /// Use for public endpoints that should return empty data gracefully.
    /// </summary>
    protected async Task<int> GetTenantIdSafeAsync()
    {
        try
        {
            var tenant = await _tenantService.GetCurrentTenantAsync();
            return tenant.Id;
        }
        catch
        {
            return -1;
        }
    }
}
