using Microsoft.AspNetCore.Mvc;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Dedicated attachment-content download path. Calls usp_TicketAttachment_GetContent — the
/// only stored procedure allowed to select the Content column — kept separate from
/// TicketsController's attachment-list endpoint (metadata only) so a ticket/queue fetch never
/// risks pulling file bytes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public AttachmentsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(string id, CancellationToken cancellationToken)
    {
        var content = await _ticketService.GetAttachmentContentAsync(id, cancellationToken);
        return content is null ? NotFound() : File(content.Content, content.ContentType, content.FileName);
    }
}
