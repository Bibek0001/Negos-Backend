namespace Diyalo.Api.Models;

public class News
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
