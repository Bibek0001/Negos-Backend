namespace Diyalo.Api.Models;

public class Testimonial
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsVisible { get; set; } = true;
}
