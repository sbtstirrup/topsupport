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
public class AgentAdminService
{
    private readonly IAgentAdminRepository _repository;
    private readonly AccountPasswordService _passwords;
    private readonly AuditService _audit;

    public AgentAdminService(IAgentAdminRepository repository, AccountPasswordService passwords, AuditService audit)
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

    /// <summary>Creates (empty Id) or edits an agent. A new agent gets a temporary password, returned once.</summary>
    public async Task<AgentAdminSaveResult> SaveAgentAsync(AgentSaveRequest request, string actorId, CancellationToken cancellationToken = default)
    {
        var result = new AgentAdminSaveResult();
        var errors = result.Errors;

        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var username = (request.Username ?? string.Empty).Trim();
        var isNew = string.IsNullOrEmpty(request.Id);

        AgentDto? existing = null;
        IReadOnlyList<AgentDto> agents = Array.Empty<AgentDto>();
        if (!isNew)
        {
            agents = await _repository.GetAllAgentsAsync(cancellationToken);
            existing = agents.FirstOrDefault(a => a.Id == request.Id)
                ?? throw new KeyNotFoundException($"Agent \"{request.Id}\" not found.");
        }

        // validateUser(F, true)
        var conflicts = await _repository.FindConflictsAsync(request.Id ?? string.Empty, email, username, cancellationToken);
        if (name.Length == 0) errors["name"] = "Enter a name.";
        else if (FieldLimits.TooLong(name, FieldLimits.PersonName) is { } nameErr) errors["name"] = nameErr;

        // Length first: the column is 256 and the regex shouldn't scan an unbounded string.
        if (FieldLimits.TooLong(email, FieldLimits.Email) is { } emailErr) errors["email"] = emailErr;
        else if (!FieldLimits.IsValidEmail(email)) errors["email"] = "Enter a valid email address.";
        else if (conflicts.EmailTaken) errors["email"] = "Someone already uses this email.";
        if (!FieldLimits.IsValidUsername(username)) errors["username"] = "Use 3 to 20 lower-case letters.";
        else if (conflicts.UsernameTaken) errors["username"] = "This username is taken.";

        if (FieldLimits.TooLong(request.Phone, FieldLimits.Phone) is { } phoneErr) errors["phone"] = phoneErr;
        else if (!FieldLimits.IsValidPhone(request.Phone)) errors["phone"] = "Use digits, spaces and + only.";
        if (FieldLimits.TooLong(request.JobTitle, FieldLimits.JobTitle) is { } jobErr) errors["jobTitle"] = jobErr;

        // Unknown departments/teams are a tampered or stale form: reject rather than silently drop.
        var departments = TicketTopics.DEPTS.Where(d => request.Departments.Contains(d)).ToList();
        if (departments.Count == 0 || request.Departments.Any(d => !TicketTopics.DEPTS.Contains(d)))
        {
            errors["departments"] = "Choose at least one department.";
        }

        var teamIds = (await _repository.GetAllTeamsAsync(cancellationToken))
            .Where(t => request.TeamIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();
        if (request.TeamIds.Distinct().Count() != teamIds.Count) errors["teams"] = "Choose a valid team.";

        if (!AgentRoles.All.Contains(request.Role)) errors["role"] = "Choose a role.";
        else if (existing is { Active: true, Role: AgentRoles.Administrator }
                 && (request.Role != AgentRoles.Administrator || !request.Active)
                 && !agents.Any(a => a.Id != existing.Id && a.Active && a.Role == AgentRoles.Administrator))
        {
            // Not in the demo: the last active Administrator can't be demoted or deactivated,
            // or nobody would be left to run the admin screens.
            errors["role"] = "Keep at least one active Administrator.";
        }

        if (errors.Count > 0) throw new ValidationException(errors);

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
            await _audit.LogAsync(actorId, "Created agent", agent.Name, AuditCategories.Permission, cancellationToken);
        }
        else
        {
            await _audit.LogAsync(actorId,
                "Edited agent (role " + agent.Role + "; departments " + string.Join(", ", agent.Departments) + ")",
                agent.Name, AuditCategories.Permission, cancellationToken);
        }

        result.Id = agent.Id;
        result.Name = agent.Name;
        return result;
    }

    /// <summary>Null if the agent doesn't exist.</summary>
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

    /// <summary>Creates (empty Id) or edits a team.</summary>
    public async Task<AgentAdminSaveResult> SaveTeamAsync(TeamSaveRequest request, string actorId, CancellationToken cancellationToken = default)
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
        var lead = request.LeadAgentId ?? string.Empty;

        if (name.Length == 0) errors["name"] = "Enter a team name.";
        else if (FieldLimits.TooLong(name, FieldLimits.TeamName) is { } nameErr) errors["name"] = nameErr;
        else if (teams.Any(t => t.Id != request.Id && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))) errors["name"] = "A team with this name already exists.";

        // Unknown department, member or lead ids are a tampered or stale form: reject rather than
        // coerce or silently drop them.
        if (!TicketTopics.DEPTS.Contains(request.Department)) errors["department"] = "Choose a department.";
        if (request.MemberAgentIds.Any(id => !agentIds.Contains(id))) errors["members"] = "Choose members from the agent list.";
        else if (members.Count == 0) errors["members"] = "Add at least one member.";
        if (lead.Length > 0 && !members.Contains(lead)) errors["lead"] = "Choose a lead from the team members.";

        if (errors.Count > 0) throw new ValidationException(errors);

        var team = new TeamDto
        {
            Id = isNew ? "t" + Guid.NewGuid().ToString("n") : request.Id,
            Name = name,
            Department = request.Department,
            LeadAgentId = lead,
            MemberAgentIds = members
        };

        await _repository.SaveTeamAsync(team, cancellationToken);
        await _audit.LogAsync(actorId,
            (isNew ? "Created" : "Edited") + " team (" + team.MemberAgentIds.Count + " members)",
            team.Name, AuditCategories.Permission, cancellationToken);

        result.Id = team.Id;
        result.Name = team.Name;
        return result;
    }

    /// <summary>The check ACT['team-del'] makes before asking for confirmation: Error is set while
    /// any ticket (any status) is still assigned to the team.</summary>
    public async Task<TeamDeleteResult> CheckTeamDeleteAsync(string teamId, CancellationToken cancellationToken = default)
        => (await CheckTeamDeleteCoreAsync(teamId, cancellationToken)).Result;

    /// <summary>Refuses (Error set) while any ticket is still assigned to the team.</summary>
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
}
