using Microsoft.AspNetCore.Mvc;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

/// <summary>
/// Base controller that resolves the current tenant on every request.
/// All tenant-scoped controllers inherit from this.
/// </summary>
[ApiController]
public abstract class TenantBaseController : ControllerBase
{
    protected readonly TenantService _tenantService;

    protected TenantBaseController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>Resolves and returns the current TenantId from the request host.</summary>
    protected async Task<int> GetTenantIdAsync()
    {
        var tenant = await _tenantService.GetCurrentTenantAsync();
        return tenant.Id;
    }
}
