namespace TopSupport.Models.People;

/// <summary>Mirrors an entry in the demo's DB.users (end-user / ticket requester).</summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string OrgId { get; set; } = string.Empty;
    public string? Office { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public bool Locked { get; set; }

    /// <summary>The demo's u.deleted soft delete: hidden from lists and pickers, but still named on old tickets.</summary>
    public bool Deleted { get; set; }
}
