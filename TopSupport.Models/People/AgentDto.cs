namespace TopSupport.Models.People;

/// <summary>Mirrors an entry in the demo's DB.agents (service desk agent/supervisor/admin).</summary>
public class AgentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>"Agent" | "Supervisor" | "Administrator" in the demo — distinct from AppRole,
    /// which governs which portal (user/agent/admin) a signed-in account lands in.</summary>
    public string Role { get; set; } = string.Empty;
    public List<string> Departments { get; set; } = new();
    public List<string> TeamIds { get; set; } = new();
    public string? JobTitle { get; set; }
    public bool Active { get; set; } = true;
    public bool Locked { get; set; }
}
