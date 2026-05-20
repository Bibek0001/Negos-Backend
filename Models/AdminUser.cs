namespace Diyalo.Api.Models;

public class AdminUser
{
    public int Id { get; set; }

    /// <summary>
    /// TenantId = 0 means SuperAdmin (platform owner, no tenant).
    /// TenantId > 0 means tenant admin (scoped to one tenant).
    /// </summary>
    public int TenantId { get; set; } = 0;

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"SuperAdmin" or "Admin"</summary>
    public string Role { get; set; } = "Admin";
}
