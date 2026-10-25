using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Tickets;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>Canned-response CRUD + placeholder preview against a real ticket. Agents and admins only.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Agent)]
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

    // Mutations attribute the audit entry to the signed-in agent; any ?actorId= the caller sends is ignored.
    // Invalid input throws ValidationException, which ApiExceptionHandler turns into a 400.

    [HttpPost]
    public async Task<ActionResult<CannedResponseDto>> Create(CannedResponseDto response, CancellationToken cancellationToken)
        => Ok(await _cannedResponses.CreateAsync(response, User.AccountId(), cancellationToken));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, CannedResponseDto response, CancellationToken cancellationToken)
    {
        response.Id = id;
        return await _cannedResponses.UpdateAsync(response, User.AccountId(), cancellationToken) ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        => await _cannedResponses.DeleteAsync(id, User.AccountId(), cancellationToken) ? Ok() : NotFound();

    /// <summary>AgentId is ignored: placeholders are filled for the signed-in agent.</summary>
    public record PreviewRequest(string? TicketId, string? AgentId);

    [HttpPost("{id}/preview")]
    public async Task<ActionResult<object>> Preview(string id, PreviewRequest request, CancellationToken cancellationToken)
    {
        var responses = await _cannedResponses.GetAllAsync(cancellationToken);
        var response = responses.FirstOrDefault(r => r.Id == id);
        if (response is null) return NotFound();

        // No ticket id: the sample values (a preview). An id that doesn't exist is a 404, never the samples.
        TicketDto? ticket = null;
        if (!string.IsNullOrEmpty(request.TicketId))
        {
            ticket = await _ticketService.GetTicketAsync(request.TicketId, cancellationToken);
            if (ticket is null) return NotFound();
        }

        var filled = await _cannedResponses.FillPlaceholdersAsync(response.Body, ticket, User.AccountId(), cancellationToken);
        return Ok(new { text = filled });
    }
}
