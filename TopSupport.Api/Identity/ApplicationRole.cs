namespace TopSupport.Api.Identity;

/// <summary>Identity's role model for TopSupport. Maps to dbo.AspNetRoles.</summary>
public class ApplicationRole
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? ConcurrencyStamp { get; set; }
}
