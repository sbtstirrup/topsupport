namespace TopSupport.Models.People;

/// <summary>Mirrors an entry in the demo's DB.assets (an IT asset tagged to a user).</summary>
public class AssetDto
{
    public string Tag { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
