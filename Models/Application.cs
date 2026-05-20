namespace Diyalo.Api.Models;

public class Application
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? Duration { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}
