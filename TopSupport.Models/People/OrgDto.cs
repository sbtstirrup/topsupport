namespace TopSupport.Models.People;

/// <summary>Mirrors an entry in the demo's DB.orgs (office/site).</summary>
public class OrgDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Note { get; set; }
}
