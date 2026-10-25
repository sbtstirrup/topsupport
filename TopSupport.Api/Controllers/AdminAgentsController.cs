using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Admin "Agents and Teams" screen: agents (all, including inactive), teams, active ticket counts,
/// agent/team create-edit, team delete and agent password reset. Invalid saves are a 400 with the
/// demo's field messages. The acting admin is always the signed-in account. Admins only.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.Admin)]
public class AdminAgentsController : ControllerBase
{
    private readonly AgentAdminService _service;

    public AdminAgentsController(AgentAdminService service)
    {
        _service = service;
    }

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> GetAgents(CancellationToken cancellationToken)
        => Ok(await _service.GetAgentsAsync(cancellationToken));

    [HttpGet("teams")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetTeams(CancellationToken cancellationToken)
        => Ok(await _service.GetTeamsAsync(cancellationToken));

    [HttpGet("agents/active-ticket-counts")]
    public async Task<ActionResult<ActiveTicketCountsDto>> GetActiveTicketCounts(CancellationToken cancellationToken)
        => Ok(await _service.GetActiveTicketCountsAsync(cancellationToken));

    [HttpPost("agents")]
    public async Task<ActionResult<AgentAdminSaveResult>> SaveAgent(AgentSaveRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SaveAgentAsync(request, User.AccountId(), cancellationToken));

    [HttpPost("agents/{id}/reset-password")]
    public async Task<ActionResult<AgentPasswordResetResult>> ResetPassword(string id, CancellationToken cancellationToken)
    {
        var result = await _service.ResetPasswordAsync(User.AccountId(), id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("teams")]
    public async Task<ActionResult<AgentAdminSaveResult>> SaveTeam(TeamSaveRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SaveTeamAsync(request, User.AccountId(), cancellationToken));

    [HttpGet("teams/{id}/delete-check")]
    public async Task<ActionResult<TeamDeleteResult>> CheckTeamDelete(string id, CancellationToken cancellationToken)
        => Ok(await _service.CheckTeamDeleteAsync(id, cancellationToken));

    [HttpDelete("teams/{id}")]
    public async Task<ActionResult<TeamDeleteResult>> DeleteTeam(string id, CancellationToken cancellationToken)
        => Ok(await _service.DeleteTeamAsync(User.AccountId(), id, cancellationToken));
}
