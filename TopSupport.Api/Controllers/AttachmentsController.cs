using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Tickets;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Dedicated attachment-content download path. Calls usp_TicketAttachment_GetContent — the
/// only stored procedure allowed to select the Content column — kept separate from
/// TicketsController's attachment-list endpoint (metadata only) so a ticket/queue fetch never
/// risks pulling file bytes. Any signed-in account; end users only for files they can see on
/// their own tickets (404 otherwise).
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

    /// <summary>
    /// Always served as a download (Content-Disposition: attachment), never inline: images
    /// (png/jpeg/gif/webp) keep their type, everything else is application/octet-stream.
    /// </summary>
    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(string id, CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && !await IsVisibleToEndUserAsync(id, cancellationToken)) return NotFound();

        var content = await _ticketService.GetAttachmentContentAsync(id, cancellationToken);
        if (content is null) return NotFound();

        var contentType = TicketAttachmentRules.ImageContentTypes.Contains(content.ContentType)
            ? content.ContentType
            : "application/octet-stream";
        // File(..., fileDownloadName) sets Content-Disposition: attachment (with a filename* for non-ASCII names).
        return File(content.Content, contentType, string.IsNullOrWhiteSpace(content.FileName) ? "file" : content.FileName);
    }

    /// <summary>
    /// One lookup of the attachment's ticket (usp_TicketAttachment_GetTicketId), then the same
    /// rules as the ticket view: the caller's own ticket, and a file EndUserTicketAccess lets
    /// them see (not on an internal note, and not an unanchored agent file).
    /// </summary>
    private async Task<bool> IsVisibleToEndUserAsync(string attachmentId, CancellationToken cancellationToken)
    {
        var ticketId = await _ticketService.GetAttachmentTicketIdAsync(attachmentId, cancellationToken);
        if (ticketId is null) return false;

        var ticket = await _ticketService.GetTicketAsync(ticketId, cancellationToken);
        return ticket is not null && User.CanSeeAttachment(ticket, attachmentId);
    }
}
