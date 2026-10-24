using TopSupport.Models.Admin;
using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Admin-side data access for agents and teams (usp_AdminAgent_*/usp_AdminTeam_*). Kept separate
/// from IAgentRepository/ITeamRepository, which serve the active-only lookups the ticket screens use.
/// </summary>
public interface IAgentAdminRepository
{
    /// <summary>Every agent, including inactive ones, in creation order.</summary>
    Task<IReadOnlyList<AgentDto>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Every team in creation order.</summary>
    Task<IReadOnlyList<TeamDto>> GetAllTeamsAsync(CancellationToken cancellationToken = default);

    Task<ActiveTicketCountsDto> GetActiveTicketCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether another user or agent (not <paramref name="id"/>) already has this email / username.</summary>
    Task<(bool EmailTaken, bool UsernameTaken)> FindConflictsAsync(string id, string email, string username, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the agent, replacing its departments and team memberships.</summary>
    Task SaveAgentAsync(AgentDto agent, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the team, replacing its members.</summary>
    Task SaveTeamAsync(TeamDto team, CancellationToken cancellationToken = default);

    /// <summary>Tickets of any status assigned to the team.</summary>
    Task<int> CountTeamTicketsAsync(string teamId, CancellationToken cancellationToken = default);

    Task DeleteTeamAsync(string teamId, CancellationToken cancellationToken = default);
}

/// <summary>Data access for dbo.RolePermissions (usp_RolePermission_GetAll/Set).</summary>
public interface IRolePermissionRepository
{
    Task<IReadOnlyList<RolePermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string department, string role, string permission, bool allowed, CancellationToken cancellationToken = default);
}
