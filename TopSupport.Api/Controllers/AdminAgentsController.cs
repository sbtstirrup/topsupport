using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Admin "Agents and Teams" screen: agents (all, including inactive), teams, active ticket counts,
/// agent/team create-edit, team delete and agent password reset. Saves return 200 with an
/// AgentAdminSaveResult whose Errors carry the demo's field validation messages.
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminAgentsController : ControllerBase
{
    private readonly IAgentAdminService _service;

    public AdminAgentsController(IAgentAdminService service)
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
    {
        try
        {
            return Ok(await _service.SaveAgentAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ActorRequest(string ActorId);

    [HttpPost("agents/{id}/reset-password")]
    public async Task<ActionResult<AgentPasswordResetResult>> ResetPassword(string id, ActorRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ResetPasswordAsync(request.ActorId, id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("teams")]
    public async Task<ActionResult<AgentAdminSaveResult>> SaveTeam(TeamSaveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SaveTeamAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("teams/{id}/delete-check")]
    public async Task<ActionResult<TeamDeleteResult>> CheckTeamDelete(string id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.CheckTeamDeleteAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("teams/{id}")]
    public async Task<ActionResult<TeamDeleteResult>> DeleteTeam(string id, [FromQuery] string actorId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.DeleteTeamAsync(actorId, id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
