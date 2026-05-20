namespace Diyalo.Api.Models;

public class Faq
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
}
