using TopSupport.Models.Enums;

namespace TopSupport.Models.Tickets;

/// <summary>
/// Mirrors a ticket in the demo's DB.tickets. Per-topic form data is kept as a free-form
/// dictionary (FieldsJson in the database) rather than fixed columns, matching how the
/// source app's topicFields/renderTopicField render a different field set per TOPICS entry
/// without needing a schema change per topic.
/// </summary>
public class TicketDto
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Zero-padded display number, e.g. "060001" (shown as #060001 in the UI).</summary>
    public string Number { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>Topic id from the TOPICS list (e.g. "hardware", "leaver", "vpn").</summary>
    public string Topic { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }

    public string RequesterUserId { get; set; } = string.Empty;
    public string AffectedUserId { get; set; } = string.Empty;
    public TicketActorRef CreatedBy { get; set; } = new();

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Updated { get; set; }

    public string? AssignedAgentId { get; set; }
    public string? AssignedTeamId { get; set; }

    /// <summary>Per-topic dynamic form answers (free-form key/value), persisted as JSON.</summary>
    public Dictionary<string, object?> Fields { get; set; } = new();

    public List<TicketTimelineEntryDto> Timeline { get; set; } = new();
    public List<TicketTaskDto> Tasks { get; set; } = new();

    /// <summary>Agent ids who are watching this ticket without being the assignee (demo's "viewers").</summary>
    public List<string> ViewerAgentIds { get; set; } = new();

    /// <summary>Attachment metadata only (no byte content) — see TicketAttachmentDto.</summary>
    public List<TicketAttachmentDto> Attachments { get; set; } = new();

    /// <summary>Earliest Reply-kind timeline entry posted by an agent (first response time),
    /// populated only by queue-returning repository calls (usp_Ticket_GetActiveForQueue /
    /// usp_Ticket_SearchForQueue) for SLA/answered calculations in QueueService; null elsewhere.</summary>
    public DateTimeOffset? FirstAgentReplyAt { get; set; }

    /// <summary>ByType ("user" | "agent") of the most recent Reply-kind entry, or null if
    /// there isn't one yet. Populated only by the same queue-returning repository calls as
    /// FirstAgentReplyAt.</summary>
    public string? LastReplyByType { get; set; }
}
