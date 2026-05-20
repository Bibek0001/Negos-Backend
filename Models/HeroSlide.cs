namespace Diyalo.Api.Models;

public class HeroSlide
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Highlight { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
}
