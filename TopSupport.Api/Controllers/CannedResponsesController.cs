using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Tickets;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>Canned-response CRUD + placeholder preview against a real ticket.</summary>
[ApiController]
[Route("api/[controller]")]
public class CannedResponsesController : ControllerBase
{
    private readonly ICannedResponseService _cannedResponses;
    private readonly ITicketService _ticketService;

    public CannedResponsesController(ICannedResponseService cannedResponses, ITicketService ticketService)
    {
        _cannedResponses = cannedResponses;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CannedResponseDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _cannedResponses.GetAllAsync(cancellationToken));

    // Mutations take an optional ?actorId= (the signed-in agent) so the audit entry is attributed.

    [HttpPost]
    public async Task<ActionResult<CannedResponseDto>> Create(CannedResponseDto response, [FromQuery] string? actorId, CancellationToken cancellationToken)
        => Ok(await _cannedResponses.CreateAsync(response, actorId, cancellationToken));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CannedResponseDto response, [FromQuery] string? actorId, CancellationToken cancellationToken)
    {
        response.Id = id;
        await _cannedResponses.UpdateAsync(response, actorId, cancellationToken);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string? actorId, CancellationToken cancellationToken)
    {
        await _cannedResponses.DeleteAsync(id, actorId, cancellationToken);
        return Ok();
    }

    public record PreviewRequest(string? TicketId, string? AgentId);

    [HttpPost("{id}/preview")]
    public async Task<ActionResult<object>> Preview(string id, PreviewRequest request, CancellationToken cancellationToken)
    {
        var responses = await _cannedResponses.GetAllAsync(cancellationToken);
        var response = responses.FirstOrDefault(r => r.Id == id);
        if (response is null) return NotFound();

        var ticket = string.IsNullOrEmpty(request.TicketId)
            ? null
            : await _ticketService.GetTicketAsync(request.TicketId, cancellationToken);

        var filled = await _cannedResponses.FillPlaceholdersAsync(response.Body, ticket, request.AgentId, cancellationToken);
        return Ok(new { text = filled });
    }
}
