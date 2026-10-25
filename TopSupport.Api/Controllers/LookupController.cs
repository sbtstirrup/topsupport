using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.People;
using TopSupport.Services;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Read-only lookup data (active users/agents/teams/assets) for Blazor dropdowns/combos
/// (PersonCombo, DynamicTopicField asset pickers, assignment modals, etc.). Thin pass-through
/// over the repositories -- no business logic lives here. Agents and teams are
/// open to any signed-in account (end-user ticket pages name the assignee); the user list is
/// agents only; end users may only list their own assets.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IAgentRepository _agents;
    private readonly ITeamRepository _teams;
    private readonly IAssetRepository _assets;

    public LookupController(IUserRepository users, IAgentRepository agents, ITeamRepository teams, IAssetRepository assets)
    {
        _users = users;
        _agents = agents;
        _teams = teams;
        _assets = assets;
    }

    [HttpGet("users")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken)
        => Ok(await _users.GetAllActiveAsync(cancellationToken));

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> GetAgents(CancellationToken cancellationToken)
        => Ok(User.ForCaller(await _agents.GetAllActiveAsync(cancellationToken)));

    [HttpGet("teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(CancellationToken cancellationToken)
        => Ok(User.ForCaller(await _teams.GetAllAsync(cancellationToken)));

    /// <summary>One user's assets (there is no "all assets" lookup).</summary>
    [HttpGet("assets")]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAssets([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId)) throw new ValidationException("userId", "Choose a user.");

        if (User.IsEndUser() && userId != User.AccountId()) return Forbid();

        return Ok(await _assets.GetByUserIdAsync(userId, cancellationToken));
    }
}
