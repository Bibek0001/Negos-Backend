namespace Diyalo.Api.Models;

/// <summary>
/// Represents a tenant (client organization) in the SaaS platform.
/// Each tenant gets their own subdomain and isolated data.
/// </summary>
public class Tenant
{
    public int Id { get; set; }

    /// <summary>Subdomain slug — e.g. "diyalo" for diyalo.negos.org</summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>Display name of the organization</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Contact email for the tenant</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether this tenant is active and can access the platform</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the tenant was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
