using System.Text.RegularExpressions;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Port of the admin "Agents and Teams" actions in topsupport-app.js: ACT['agent-save'] (with
/// validateUser(F, true)), ACT['team-save'], ACT['team-del'] and ACT['user-reset'] for agents.
/// Validation messages and audit wording match the demo exactly.
/// </summary>
public interface IAgentAdminService
{
    Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default);

    Task<ActiveTicketCountsDto> GetActiveTicketCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates (empty Id) or edits an agent. A new agent gets a temporary password, returned once.</summary>
    Task<AgentAdminSaveResult> SaveAgentAsync(AgentSaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Null if the agent doesn't exist.</summary>
    Task<AgentPasswordResetResult?> ResetPasswordAsync(string actorId, string agentId, CancellationToken cancellationToken = default);

    /// <summary>Creates (empty Id) or edits a team.</summary>
    Task<AgentAdminSaveResult> SaveTeamAsync(TeamSaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>The check ACT['team-del'] makes before asking for confirmation: Error is set while
    /// any ticket (any status) is still assigned to the team.</summary>
    Task<TeamDeleteResult> CheckTeamDeleteAsync(string teamId, CancellationToken cancellationToken = default);

    /// <summary>Refuses (Error set) while any ticket is still assigned to the team.</summary>
    Task<TeamDeleteResult> DeleteTeamAsync(string actorId, string teamId, CancellationToken cancellationToken = default);
}

public partial class AgentAdminService : IAgentAdminService
{
    private readonly IAgentAdminRepository _repository;
    private readonly IAccountPasswordService _passwords;
    private readonly IAuditService _audit;

    public AgentAdminService(IAgentAdminRepository repository, IAccountPasswordService passwords, IAuditService audit)
    {
        _repository = repository;
        _passwords = passwords;
        _audit = audit;
    }

    public Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAgentsAsync(cancellationToken);

    public Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllTeamsAsync(cancellationToken);

    public Task<ActiveTicketCountsDto> GetActiveTicketCountsAsync(CancellationToken cancellationToken = default)
        => _repository.GetActiveTicketCountsAsync(cancellationToken);

    public async Task<AgentAdminSaveResult> SaveAgentAsync(AgentSaveRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AgentAdminSaveResult();
        var errors = result.Errors;

        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var username = (request.Username ?? string.Empty).Trim();
        var isNew = string.IsNullOrEmpty(request.Id);

        AgentDto? existing = null;
        if (!isNew)
        {
            existing = (await _repository.GetAllAgentsAsync(cancellationToken)).FirstOrDefault(a => a.Id == request.Id)
                ?? throw new KeyNotFoundException($"Agent \"{request.Id}\" not found.");
        }

        // validateUser(F, true)
        var conflicts = await _repository.FindConflictsAsync(request.Id ?? string.Empty, email, username, cancellationToken);
        if (name.Length == 0) errors["name"] = "Enter a name.";
        if (!EmailPattern().IsMatch(email)) errors["email"] = "Enter a valid email address.";
        else if (conflicts.EmailTaken) errors["email"] = "Someone already uses this email.";
        if (!UsernamePattern().IsMatch(username)) errors["username"] = "Use 3 to 20 lower-case letters.";
        else if (conflicts.UsernameTaken) errors["username"] = "This username is taken.";

        var departments = TicketTopics.DEPTS.Where(d => request.Departments.Contains(d)).ToList();
        if (departments.Count == 0) errors["departments"] = "Choose at least one department.";
        if (!AgentRoles.All.Contains(request.Role)) errors["role"] = "Choose a role.";

        if (errors.Count > 0) return result;

        var teamIds = (await _repository.GetAllTeamsAsync(cancellationToken))
            .Where(t => request.TeamIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();

        var agent = new AgentDto
        {
            Id = isNew ? "a" + Guid.NewGuid().ToString("n") : request.Id!,
            Name = name,
            Email = email,
            Username = username,
            Phone = (request.Phone ?? string.Empty).Trim(),
            JobTitle = (request.JobTitle ?? string.Empty).Trim(),
            Role = request.Role,
            Departments = departments,
            TeamIds = teamIds,
            Active = request.Active,
            Locked = existing?.Locked ?? false
        };

        await _repository.SaveAgentAsync(agent, cancellationToken);

        if (isNew)
        {
            var password = _passwords.GenerateTemporary();
            await _passwords.SetAsync(agent.Id, password, cancellationToken);
            result.TemporaryPassword = password;
            await _audit.LogAsync(request.ActorId, "Created agent", agent.Name, AuditCategories.Permission, cancellationToken);
        }
        else
        {
            await _audit.LogAsync(request.ActorId,
                "Edited agent (role " + agent.Role + "; departments " + string.Join(", ", agent.Departments) + ")",
                agent.Name, AuditCategories.Permission, cancellationToken);
        }

        result.Id = agent.Id;
        result.Name = agent.Name;
        return result;
    }

    public async Task<AgentPasswordResetResult?> ResetPasswordAsync(string actorId, string agentId, CancellationToken cancellationToken = default)
    {
        var agent = (await _repository.GetAllAgentsAsync(cancellationToken)).FirstOrDefault(a => a.Id == agentId);
        if (agent is null) return null;

        // ACT['user-reset']: new temporary password, account unlocked (SetAsync unlocks), audited
        // under the "User" category even for agents.
        var password = _passwords.GenerateTemporary();
        await _passwords.SetAsync(agent.Id, password, cancellationToken);
        await _audit.LogAsync(actorId, "Reset password", agent.Name, AuditCategories.User, cancellationToken);

        return new AgentPasswordResetResult { Name = agent.Name, Username = agent.Username, TemporaryPassword = password };
    }

    public async Task<AgentAdminSaveResult> SaveTeamAsync(TeamSaveRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AgentAdminSaveResult();
        var errors = result.Errors;
        var isNew = string.IsNullOrEmpty(request.Id);
        var name = (request.Name ?? string.Empty).Trim();

        var teams = await _repository.GetAllTeamsAsync(cancellationToken);
        if (!isNew && teams.All(t => t.Id != request.Id))
        {
            throw new KeyNotFoundException($"Team \"{request.Id}\" not found.");
        }

        var agentIds = (await _repository.GetAllAgentsAsync(cancellationToken)).Select(a => a.Id).ToHashSet();
        var members = request.MemberAgentIds.Where(agentIds.Contains).Distinct().ToList();

        if (name.Length == 0) errors["name"] = "Enter a team name.";
        else if (teams.Any(t => t.Id != request.Id && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))) errors["name"] = "A team with this name already exists.";
        if (members.Count == 0) errors["members"] = "Add at least one member.";

        if (errors.Count > 0) return result;

        var team = new TeamDto
        {
            Id = isNew ? "t" + Guid.NewGuid().ToString("n") : request.Id,
            Name = name,
            Department = TicketTopics.DEPTS.Contains(request.Department) ? request.Department : TicketTopics.DEPTS[0],
            LeadAgentId = members.Contains(request.LeadAgentId) ? request.LeadAgentId : string.Empty,
            MemberAgentIds = members
        };

        await _repository.SaveTeamAsync(team, cancellationToken);
        await _audit.LogAsync(request.ActorId,
            (isNew ? "Created" : "Edited") + " team (" + team.MemberAgentIds.Count + " members)",
            team.Name, AuditCategories.Permission, cancellationToken);

        result.Id = team.Id;
        result.Name = team.Name;
        return result;
    }

    public async Task<TeamDeleteResult> CheckTeamDeleteAsync(string teamId, CancellationToken cancellationToken = default)
        => (await CheckTeamDeleteCoreAsync(teamId, cancellationToken)).Result;

    public async Task<TeamDeleteResult> DeleteTeamAsync(string actorId, string teamId, CancellationToken cancellationToken = default)
    {
        var (team, check) = await CheckTeamDeleteCoreAsync(teamId, cancellationToken);
        if (check.Error is not null) return check;

        await _repository.DeleteTeamAsync(teamId, cancellationToken);
        await _audit.LogAsync(actorId, "Deleted team", team.Name, AuditCategories.Permission, cancellationToken);
        return new TeamDeleteResult();
    }

    private async Task<(TeamDto Team, TeamDeleteResult Result)> CheckTeamDeleteCoreAsync(string teamId, CancellationToken cancellationToken)
    {
        var team = (await _repository.GetAllTeamsAsync(cancellationToken)).FirstOrDefault(t => t.Id == teamId)
            ?? throw new KeyNotFoundException($"Team \"{teamId}\" not found.");

        var tickets = await _repository.CountTeamTicketsAsync(teamId, cancellationToken);
        return (team, tickets > 0
            ? new TeamDeleteResult { Error = $"{team.Name} has {tickets} tickets. Reassign them before deleting the team." }
            : new TeamDeleteResult());
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[a-z]{3,20}$")]
    private static partial Regex UsernamePattern();
}
