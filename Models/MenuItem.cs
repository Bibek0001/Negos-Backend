namespace Diyalo.Api.Models;

public class MenuItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    /// <summary>Null = top-level menu item. Non-null = submenu child of the given parent.</summary>
    public int? ParentId { get; set; }
}
