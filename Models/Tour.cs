namespace Diyalo.Api.Models;

public class Tour
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
}
