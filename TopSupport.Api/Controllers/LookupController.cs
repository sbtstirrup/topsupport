using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Read-only lookup data (active users/agents/teams/assets) for Blazor dropdowns/combos
/// (PersonCombo, DynamicTopicField asset pickers, assignment modals, etc.). Thin pass-through
/// over the repositories built in Tier 2 -- no business logic lives here.
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
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken)
        => Ok(await _users.GetAllActiveAsync(cancellationToken));

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> GetAgents(CancellationToken cancellationToken)
        => Ok(await _agents.GetAllActiveAsync(cancellationToken));

    [HttpGet("teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(CancellationToken cancellationToken)
        => Ok(await _teams.GetAllAsync(cancellationToken));

    /// <summary>
    /// Assets for a given user. NOTE: IAssetRepository only exposes GetByTagAsync/
    /// GetByUserIdAsync (Tier 2) -- there is no "get all assets" stored procedure, and adding
    /// one is out of scope for this Tier 3 pass (would require touching TopSupport.Database/
    /// TopSupport.Services). userId is therefore required; see the Tier 3 report's gaps
    /// section re: DevicePanel, which would need a full asset list or a by-tag ticket query.
    /// </summary>
    [HttpGet("assets")]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAssets([FromQuery] string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "userId is required." });
        }

        return Ok(await _assets.GetByUserIdAsync(userId, cancellationToken));
    }
}
