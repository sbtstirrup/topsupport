using TopSupport.Models.Enums;

namespace TopSupport.Models.Tickets;

/// <summary>
/// One row of a ticket's timeline, mirroring the entries appended by addEntry/addEvent in
/// topsupport-app.js: a system/user event line, an agent-only note, or a public reply.
/// </summary>
public class TicketTimelineEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public TicketTimelineEntryKind Kind { get; set; }
    public DateTimeOffset At { get; set; }
    public TicketActorRef By { get; set; } = new();

    /// <summary>Plain text for Kind == Event; sanitized HTML for Note/Reply (see cleanHTML in the demo).</summary>
    public string? Text { get; set; }
    public string? Html { get; set; }
}
