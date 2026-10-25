using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Business-facing ticket operations consumed by TopSupport.Api's controllers. Mirrors the
/// ticket helpers in topsupport-app.js (createTicket, changeStatus, assignTicket, addEntry/
/// addEvent, ACT['user-reply']/ACT['agent-reply']) one layer up from raw data access.
/// Ticket merge is explicitly out of scope — no merge method exists here.
/// </summary>
public interface ITicketService
{
    Task<TicketDto?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default);

    /// <summary>"My Tickets": every ticket where the user is requester or affected.</summary>
    Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(string requesterUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates via ITicketFormValidator first (agentMode = createdBy.Type == "agent"); on
    /// failure throws ValidationException. Numbers via usp_Ticket_NextNumber (zero-padded
    /// to 6 digits, "T" prefix), assigns the department's default team, leaves agent null,
    /// forces status Open, derives priority from IMPACT_PRI when the topic has an impact
    /// field, and seeds the "Created by"/"Assigned to team" timeline events (plus the leaver
    /// checklist event + 4 tasks for topic == "leaver") exactly as createTicket() does.
    /// </summary>
    Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, TicketActorRef createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws TicketClosedException if the ticket is Closed. Posts the reply, then reopens
    /// with the demo's exact (intentionally asymmetric) wording: Resolved -> "Reopened by X",
    /// AwaitingUser -> "Status changed from Awaiting User to Open by X". Any other status is
    /// left unchanged.
    /// </summary>
    Task<TicketDto> AddUserReplyAsync(string ticketId, AddUserReplyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a public reply or internal note. For a public reply with StatusAfterSending set,
    /// changes status quietly (no extra audit-style event beyond the status-change event
    /// itself) afterward, matching ACT['agent-reply'].
    /// </summary>
    Task<TicketDto> AddAgentReplyAsync(string ticketId, AddAgentReplyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// No-op if the status is unchanged. Event wording branches exactly as changeStatus(t, s):
    /// closing (Resolved/Closed) -> "Closed by X with status {s}"; reopening from Resolved/
    /// Closed -> "Reopened by X"; otherwise -> "Status changed from {prev} to {s} by X".
    /// </summary>
    Task<TicketDto> ChangeStatusAsync(string ticketId, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    Task<TicketDto> ChangePriorityAsync(string ticketId, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    Task<TicketDto> ChangeTopicAsync(string ticketId, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// The agent "Change" modal (ACT['change-go']): applies whichever of priority/topic/status
    /// differ (status quietly), each with its own timeline event, then writes ONE
    /// "Changed ticket properties" audit entry if anything changed.
    /// </summary>
    Task<TicketDto> ChangePropertiesAsync(string ticketId, string status, string priority, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    /// <summary>Defaults the ticket's team to the agent's first team if it has none yet (assignTicket's un-claim-safe default).</summary>
    Task<TicketDto> AssignToAgentAsync(string ticketId, string agentId, TicketActorRef assignedBy, CancellationToken cancellationToken = default);

    /// <summary>Clears any individually-assigned agent (assignTicket's team-assign un-claim behavior).</summary>
    Task<TicketDto> AssignToTeamAsync(string ticketId, string teamId, TicketActorRef assignedBy, CancellationToken cancellationToken = default);

    /// <summary>One-click claim: assigns the acting agent, and if the ticket is still Open, quietly moves it to InProgress.</summary>
    Task<TicketDto> ClaimAsync(string ticketId, string agentId, CancellationToken cancellationToken = default);

    Task BulkChangeStatusAsync(IEnumerable<string> ticketIds, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    Task BulkChangePriorityAsync(IEnumerable<string> ticketIds, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    Task BulkAssignAsync(IEnumerable<string> ticketIds, string kind, string id, TicketActorRef assignedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-close (ACT['close-go']): adds the optional internal note, then sets each ticket to
    /// <paramref name="status"/> (Resolved or Closed). Tickets already Closed, or already at that
    /// status, are skipped. Returns how many tickets were closed.
    /// </summary>
    Task<int> BulkCloseAsync(IEnumerable<string> ticketIds, string status, string? note, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    Task<TicketTaskDto> AddTaskAsync(string ticketId, string text, string assigneeAgentId, DateOnly? due, TicketActorRef addedBy, CancellationToken cancellationToken = default);

    /// <summary>Throws KeyNotFoundException when the ticket, or that task on it, doesn't exist.</summary>
    Task<TicketTaskDto> SetTaskDoneAsync(string ticketId, string taskId, bool done, TicketActorRef changedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and stores one file (see TicketAttachmentRules): the ticket must exist
    /// (KeyNotFoundException) and not be Closed (TicketClosedException); the extension must be
    /// allow-listed, the file non-empty and at most 10MB (AttachmentTooLargeException), and the
    /// ticket under its file-count/total-size caps; a timelineEntryId must be on the same ticket
    /// and, for an end user, one of their own replies. Other failures throw ArgumentException. The
    /// file name is sanitised and <paramref name="contentType"/> is ignored -- the stored type
    /// comes from the extension. Audited as "Attached file".
    /// </summary>
    Task<TicketAttachmentDto> AddAttachmentAsync(
        string ticketId,
        string? timelineEntryId,
        string fileName,
        string contentType,
        byte[] content,
        TicketActorRef uploadedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketAttachmentDto>> GetAttachmentsAsync(string ticketId, CancellationToken cancellationToken = default);

    Task<TicketAttachmentContentDto?> GetAttachmentContentAsync(string attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The ticket an attachment belongs to, or null when the attachment doesn't exist.</summary>
    Task<string?> GetAttachmentTicketIdAsync(string attachmentId, CancellationToken cancellationToken = default);
}
