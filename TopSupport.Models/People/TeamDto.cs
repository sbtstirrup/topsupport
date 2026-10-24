namespace TopSupport.Models.People;

/// <summary>Mirrors an entry in the demo's DB.teams.</summary>
public class TeamDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LeadAgentId { get; set; } = string.Empty;
    public List<string> MemberAgentIds { get; set; } = new();
    public string Department { get; set; } = string.Empty;
}
