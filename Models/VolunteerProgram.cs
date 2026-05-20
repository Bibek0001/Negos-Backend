namespace Diyalo.Api.Models;

public class VolunteerProgram
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = "Volunteering";
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
}
