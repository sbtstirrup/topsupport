namespace TopSupport.Models.Admin;

/// <summary>The agent roles in the demo (AGENT_ROLES in topsupport-app.js), in display order.</summary>
public static class AgentRoles
{
    public const string Agent = "Agent";
    public const string Supervisor = "Supervisor";
    public const string Administrator = "Administrator";

    public static readonly IReadOnlyList<string> All = new[] { Agent, Supervisor, Administrator };
}

/// <summary>The ticket permissions on the Roles and Permissions grid (PERMS in topsupport-app.js):
/// stored key and column label, in display order.</summary>
public static class RolePermissionKeys
{
    public static readonly IReadOnlyList<(string Key, string Label)> All = new[]
    {
        ("assign", "Assign"), ("transfer", "Transfer"), ("close", "Close"), ("delete", "Delete"), ("edit", "Edit")
    };
}

/// <summary>Form state of MODALS.agentedit / ACT['agent-save']. Id is empty to create.</summary>
public class AgentSaveRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Role { get; set; } = AgentRoles.Agent;
    public List<string> Departments { get; set; } = new();
    public List<string> TeamIds { get; set; } = new();
    public bool Active { get; set; } = true;
}

/// <summary>Form state of MODALS.teamedit / ACT['team-save']. Id is empty to create.</summary>
public class TeamSaveRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string LeadAgentId { get; set; } = string.Empty;
    public List<string> MemberAgentIds { get; set; } = new();
}

/// <summary>
/// Result of an agent/team save. Errors maps a form field key ("name", "email", "username",
/// "departments", "members") to the demo's validation message; when it is empty the save
/// succeeded. TemporaryPassword is set only when a new agent was created (shown once).
/// </summary>
public class AgentAdminSaveResult
{
    public Dictionary<string, string> Errors { get; set; } = new();
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }

    public bool Succeeded => Errors.Count == 0;
}

/// <summary>Active (Open / In Progress / Awaiting User) ticket counts keyed by agent id and team id.</summary>
public class ActiveTicketCountsDto
{
    public Dictionary<string, int> ByAgent { get; set; } = new();
    public Dictionary<string, int> ByTeam { get; set; } = new();
}

/// <summary>Result of ACT['team-del']: Error is the demo's toast text when the team still has tickets.</summary>
public class TeamDeleteResult
{
    public string? Error { get; set; }
}

/// <summary>Result of a password reset: the temporary password to show once.</summary>
public class AgentPasswordResetResult
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}

/// <summary>One cell of DB.perms[department][role][permission].</summary>
public class RolePermissionDto
{
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public bool Allowed { get; set; }
}

public class SetRolePermissionRequest
{
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public bool Allowed { get; set; }
}
