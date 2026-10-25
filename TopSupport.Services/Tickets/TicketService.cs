using TopSupport.Models.Admin;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Real implementation of ITicketService, porting createTicket/changeStatus/assignTicket/
/// addEntry/addEvent and the ACT['user-reply']/ACT['agent-reply'] handlers from
/// topsupport-app.js (lines ~404-439, ~1343-1352, ~1591-1600) one layer above raw data
/// access. No merge method anywhere — ticket merge is explicitly out of scope.
/// Each mutation also writes the same audit(...) entry the demo does (category Ticket),
/// attributed to the acting account on the request.
/// </summary>
public class TicketService : ITicketService
{
    private const long MaxAttachmentSizeBytes = 10L * 1024 * 1024;

    // Ported verbatim from LEAVER_TASKS in topsupport-app.js: the first two tasks always go
    // to agent a1, the last two to agent a5. This is demo-specific hardcoding, not a
    // configurable business rule -- intentionally not generalised here.
    private static readonly (string Text, string AssigneeAgentId)[] LeaverTasks =
    {
        ("Disable account", "a1"),
        ("Recover laptop", "a1"),
        ("Remove licences", "a5"),
        ("Archive mailbox", "a5")
    };

    private readonly ITicketRepository _ticketRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketAttachmentRepository _attachmentRepository;
    private readonly ITicketFormValidator _validator;
    private readonly IAuditService _audit;

    public TicketService(
        ITicketRepository ticketRepository,
        ITeamRepository teamRepository,
        IAgentRepository agentRepository,
        IUserRepository userRepository,
        ITicketAttachmentRepository attachmentRepository,
        ITicketFormValidator validator,
        IAuditService audit)
    {
        _ticketRepository = ticketRepository;
        _teamRepository = teamRepository;
        _agentRepository = agentRepository;
        _userRepository = userRepository;
        _attachmentRepository = attachmentRepository;
        _validator = validator;
        _audit = audit;
    }

    public Task<TicketDto?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
        => _ticketRepository.GetByIdAsync(ticketId, cancellationToken);

    public Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(string requesterUserId, CancellationToken cancellationToken = default)
        => _ticketRepository.GetByRequesterOrAffectedAsync(requesterUserId, cancellationToken);

    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, TicketActorRef createdBy, CancellationToken cancellationToken = default)
    {
        var agentMode = createdBy.Type == "agent";
        var validation = _validator.Validate(request, agentMode);
        if (!validation.IsValid)
        {
            throw new TicketValidationException(validation);
        }

        var topicDef = TicketTopics.Find(request.Topic)
            ?? throw new ArgumentException($"Unknown topic '{request.Topic}'.", nameof(request));

        var now = DateTimeOffset.UtcNow;
        var nextNumber = await _ticketRepository.GetNextNumberAsync(cancellationToken);
        var number = nextNumber.ToString("D6");
        var id = "T" + number;

        var priority = TicketPriority.Normal;
        if (request.Fields.TryGetValue("impact", out var impactValue) &&
            impactValue?.ToString() is string impact &&
            TicketFieldOptionSets.IMPACT_PRI.TryGetValue(impact, out var priorityName))
        {
            priority = Enum.Parse<TicketPriority>(priorityName);
        }

        var teams = await _teamRepository.GetAllAsync(cancellationToken);
        var defaultTeam = teams.FirstOrDefault(t => t.Department == topicDef.Department)
            ?? throw new InvalidOperationException($"No team found for department '{topicDef.Department}'.");

        var ticket = new TicketDto
        {
            Id = id,
            Number = number,
            Subject = request.Subject,
            Topic = request.Topic,
            Department = topicDef.Department,
            Status = TicketStatus.Open,
            Priority = priority,
            RequesterUserId = request.RequesterUserId,
            AffectedUserId = request.AffectedUserId,
            CreatedBy = createdBy,
            Created = now,
            Updated = now,
            AssignedAgentId = null,
            AssignedTeamId = defaultTeam.Id,
            Fields = request.Fields
        };

        await _ticketRepository.CreateAsync(ticket, cancellationToken);

        var requester = await _userRepository.GetByIdAsync(request.RequesterUserId, cancellationToken);
        var requesterShortName = ShortName(requester?.Name);
        var creatorShortName = agentMode ? await ShortNameForActorAsync(createdBy, cancellationToken) : requesterShortName;

        var createdText = agentMode
            ? $"Created by {creatorShortName} on behalf of {requesterShortName}"
            : $"Created by {requesterShortName}";

        await AppendEventAsync(ticket, createdBy, createdText, now, cancellationToken);
        await AppendEventAsync(ticket, TicketActorRef_System, $"Assigned to {defaultTeam.Name}", now, cancellationToken);

        if (request.Topic == "leaver")
        {
            DateOnly? due = request.Fields.TryGetValue("leavingDate", out var leavingDateValue) && leavingDateValue != null
                ? ParseDateOnly(leavingDateValue.ToString())
                : null;

            foreach (var (text, assigneeAgentId) in LeaverTasks)
            {
                var task = await _ticketRepository.InsertTaskAsync(new TicketTaskDto
                {
                    Id = Guid.NewGuid().ToString("n"),
                    TicketId = ticket.Id,
                    Text = text,
                    AssigneeAgentId = assigneeAgentId,
                    Due = due,
                    Done = false
                }, cancellationToken);
                ticket.Tasks.Add(task);
            }

            await AppendEventAsync(ticket, TicketActorRef_System, "Leaver checklist added (4 tasks)", now, cancellationToken);
        }

        await AuditAsync(createdBy, "Created ticket", ticket, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> AddUserReplyAsync(string ticketId, AddUserReplyRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        if (ticket.Status == TicketStatus.Closed)
        {
            throw new TicketClosedException("This ticket is closed. You can't reply to it.");
        }

        var by = new TicketActorRef { Type = "user", Id = request.UserId };
        var now = DateTimeOffset.UtcNow;

        await AppendReplyAsync(ticket, by, request.Html, now, isNote: false, cancellationToken);

        var shortName = await ShortNameForActorAsync(by, cancellationToken);
        if (ticket.Status == TicketStatus.Resolved)
        {
            ticket.Status = TicketStatus.Open;
            await _ticketRepository.UpdateStatusAsync(ticketId, ticket.Status.ToString(), now, cancellationToken);
            await AppendEventAsync(ticket, by, $"Reopened by {shortName}", now, cancellationToken);
        }
        else if (ticket.Status == TicketStatus.AwaitingUser)
        {
            ticket.Status = TicketStatus.Open;
            await _ticketRepository.UpdateStatusAsync(ticketId, ticket.Status.ToString(), now, cancellationToken);
            await AppendEventAsync(ticket, by, $"Status changed from Awaiting User to Open by {shortName}", now, cancellationToken);
        }

        await AuditAsync(by, "Replied to ticket", ticket, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> AddAgentReplyAsync(string ticketId, AddAgentReplyRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var by = new TicketActorRef { Type = "agent", Id = request.AgentId };
        var now = DateTimeOffset.UtcNow;

        await AppendReplyAsync(ticket, by, request.Html, now, request.IsNote, cancellationToken);

        if (!request.IsNote && !string.IsNullOrEmpty(request.StatusAfterSending))
        {
            await ChangeStatusInternalAsync(ticket, request.StatusAfterSending, by, now, quiet: true, cancellationToken);
        }

        await AuditAsync(by, request.IsNote ? "Added internal note" : "Sent public reply", ticket, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> ChangeStatusAsync(string ticketId, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        await ChangeStatusInternalAsync(ticket, status, changedBy, DateTimeOffset.UtcNow, quiet: false, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> ChangePriorityAsync(string ticketId, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var prev = ticket.Priority;
        var next = ParseDefined<TicketPriority>(priority, nameof(priority));
        if (prev == next) return ticket;

        ticket.Priority = next;
        await _ticketRepository.UpdatePriorityAsync(ticketId, ticket.Priority.ToString(), now, cancellationToken);

        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        await AppendEventAsync(ticket, changedBy, $"Priority changed from {prev} to {next} by {shortName}", now, cancellationToken);

        await AuditAsync(changedBy, $"Changed priority to {next}", ticket, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> ChangeTopicAsync(string ticketId, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var topicDef = TicketTopics.Find(topic) ?? throw new ArgumentException($"Unknown topic '{topic}'.", nameof(topic));
        var now = DateTimeOffset.UtcNow;

        ticket.Topic = topic;
        ticket.Department = topicDef.Department;
        await _ticketRepository.UpdateTopicAsync(ticketId, topic, topicDef.Department, now, cancellationToken);

        return ticket;
    }

    public async Task<TicketDto> ChangePropertiesAsync(string ticketId, string status, string priority, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        var changed = 0;

        var nextPriority = ParseDefined<TicketPriority>(priority, nameof(priority));
        if (nextPriority != ticket.Priority)
        {
            var prev = ticket.Priority;
            ticket.Priority = nextPriority;
            await _ticketRepository.UpdatePriorityAsync(ticketId, ticket.Priority.ToString(), now, cancellationToken);
            await AppendEventAsync(ticket, changedBy, $"Priority changed from {prev} to {nextPriority} by {shortName}", now, cancellationToken);
            changed++;
        }

        if (topic != ticket.Topic)
        {
            var topicDef = TicketTopics.Find(topic) ?? throw new ArgumentException($"Unknown topic '{topic}'.", nameof(topic));
            var prevLabel = TicketTopics.Find(ticket.Topic)?.Label ?? ticket.Topic;
            ticket.Topic = topic;
            ticket.Department = topicDef.Department;
            await _ticketRepository.UpdateTopicAsync(ticketId, topic, topicDef.Department, now, cancellationToken);
            await AppendEventAsync(ticket, changedBy, $"Help topic changed from {prevLabel} to {topicDef.Label} by {shortName}", now, cancellationToken);
            changed++;
        }

        if (ParseDefined<TicketStatus>(status, nameof(status)) != ticket.Status)
        {
            await ChangeStatusInternalAsync(ticket, status, changedBy, now, quiet: true, cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            await AuditAsync(changedBy, "Changed ticket properties", ticket, cancellationToken);
        }

        return ticket;
    }

    public async Task<TicketDto> AssignToAgentAsync(string ticketId, string agentId, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken)
            ?? throw new ArgumentException($"Unknown agent '{agentId}'.", nameof(agentId));
        var now = DateTimeOffset.UtcNow;

        ticket.AssignedAgentId = agentId;
        if (ticket.AssignedTeamId == null && agent.TeamIds.Count > 0)
        {
            ticket.AssignedTeamId = agent.TeamIds[0];
        }

        await _ticketRepository.AssignAsync(ticketId, ticket.AssignedAgentId, ticket.AssignedTeamId, now, cancellationToken);
        await AppendEventAsync(ticket, assignedBy, $"Assigned to {ShortName(agent.Name)}", now, cancellationToken);
        await AuditAsync(assignedBy, $"Assigned ticket to {ShortName(agent.Name)}", ticket, cancellationToken);

        return ticket;
    }

    public async Task<TicketDto> AssignToTeamAsync(string ticketId, string teamId, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new ArgumentException($"Unknown team '{teamId}'.", nameof(teamId));
        var now = DateTimeOffset.UtcNow;

        ticket.AssignedTeamId = teamId;
        ticket.AssignedAgentId = null;

        await _ticketRepository.AssignAsync(ticketId, null, teamId, now, cancellationToken);
        await AppendEventAsync(ticket, assignedBy, $"Assigned to {team.Name}", now, cancellationToken);
        await AuditAsync(assignedBy, $"Assigned ticket to {team.Name}", ticket, cancellationToken);

        return ticket;
    }

    public async Task<TicketDto> ClaimAsync(string ticketId, string agentId, CancellationToken cancellationToken = default)
    {
        var by = new TicketActorRef { Type = "agent", Id = agentId };
        var ticket = await AssignToAgentAsync(ticketId, agentId, by, cancellationToken);

        if (ticket.Status == TicketStatus.Open)
        {
            // "quiet" per assignTicket's caller (ACT['t-claim']): status flips with no
            // separate status-change event beyond the assignment event just added.
            ticket.Status = TicketStatus.InProgress;
            await _ticketRepository.UpdateStatusAsync(ticketId, ticket.Status.ToString(), DateTimeOffset.UtcNow, cancellationToken);
        }

        return ticket;
    }

    public async Task BulkChangeStatusAsync(IEnumerable<string> ticketIds, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        foreach (var id in ticketIds)
        {
            await ChangeStatusAsync(id, status, changedBy, cancellationToken);
        }
    }

    public async Task BulkChangePriorityAsync(IEnumerable<string> ticketIds, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        foreach (var id in ticketIds)
        {
            await ChangePriorityAsync(id, priority, changedBy, cancellationToken);
        }
    }

    public async Task BulkAssignAsync(IEnumerable<string> ticketIds, string kind, string id, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        foreach (var ticketId in ticketIds)
        {
            if (kind == "agent")
            {
                await AssignToAgentAsync(ticketId, id, assignedBy, cancellationToken);
            }
            else
            {
                await AssignToTeamAsync(ticketId, id, assignedBy, cancellationToken);
            }
        }
    }

    public async Task BulkCloseAsync(IEnumerable<string> ticketIds, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        foreach (var id in ticketIds)
        {
            await ChangeStatusAsync(id, TicketStatus.Resolved.ToString(), changedBy, cancellationToken);
        }
    }

    public Task<TicketTaskDto> AddTaskAsync(string ticketId, string text, string assigneeAgentId, DateOnly? due, CancellationToken cancellationToken = default)
        => _ticketRepository.InsertTaskAsync(new TicketTaskDto
        {
            Id = Guid.NewGuid().ToString("n"),
            TicketId = ticketId,
            Text = text,
            AssigneeAgentId = assigneeAgentId,
            Due = due,
            Done = false
        }, cancellationToken);

    public Task SetTaskDoneAsync(string taskId, bool done, CancellationToken cancellationToken = default)
        => _ticketRepository.SetTaskDoneAsync(taskId, done, cancellationToken);

    public async Task<TicketAttachmentDto> AddAttachmentAsync(
        string ticketId,
        string? timelineEntryId,
        string fileName,
        string contentType,
        byte[] content,
        TicketActorRef uploadedBy,
        CancellationToken cancellationToken = default)
    {
        if (content.LongLength > MaxAttachmentSizeBytes)
        {
            throw new AttachmentTooLargeException($"'{fileName}' is over 10 MB and was not added.");
        }

        return await _attachmentRepository.InsertAsync(
            ticketId, timelineEntryId, fileName, contentType, content, uploadedBy, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<IReadOnlyList<TicketAttachmentDto>> GetAttachmentsAsync(string ticketId, CancellationToken cancellationToken = default)
        => _attachmentRepository.GetMetadataByTicketAsync(ticketId, cancellationToken);

    public Task<TicketAttachmentContentDto?> GetAttachmentContentAsync(string attachmentId, CancellationToken cancellationToken = default)
        => _attachmentRepository.GetContentAsync(attachmentId, cancellationToken);

    // ---------- internal helpers ----------

    private static readonly TicketActorRef TicketActorRef_System = new() { Type = "system" };

    private async Task<TicketDto> RequireTicketAsync(string ticketId, CancellationToken cancellationToken)
        => await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket '{ticketId}' was not found.");

    /// <summary>changeStatus(t, s, quiet): <paramref name="quiet"/> suppresses only the audit entry,
    /// never the timeline event (used when the change is part of a larger audited action).</summary>
    private async Task ChangeStatusInternalAsync(TicketDto ticket, string status, TicketActorRef changedBy, DateTimeOffset now, bool quiet, CancellationToken cancellationToken)
    {
        var next = ParseDefined<TicketStatus>(status, nameof(status));
        if (ticket.Status == next) return;

        var prev = ticket.Status;
        ticket.Status = next;
        await _ticketRepository.UpdateStatusAsync(ticket.Id, next.ToString(), now, cancellationToken);

        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        string text;
        if (next is TicketStatus.Resolved or TicketStatus.Closed)
        {
            text = $"Closed by {shortName} with status {FormatStatus(next)}";
        }
        else if (prev is TicketStatus.Resolved or TicketStatus.Closed)
        {
            text = $"Reopened by {shortName}";
        }
        else
        {
            text = $"Status changed from {FormatStatus(prev)} to {FormatStatus(next)} by {shortName}";
        }

        await AppendEventAsync(ticket, changedBy, text, now, cancellationToken);

        if (!quiet)
        {
            await AuditAsync(changedBy, $"Changed status to {FormatStatus(next)}", ticket, cancellationToken);
        }
    }

    /// <summary>audit(action, '#' + t.num, 'Ticket'), attributed to the acting account ("system" when there is none).</summary>
    private Task AuditAsync(TicketActorRef actor, string action, TicketDto ticket, CancellationToken cancellationToken)
        => _audit.LogAsync(actor.Type == "system" || actor.Id == null ? "system" : actor.Id, action, "#" + ticket.Number, AuditCategories.Ticket, cancellationToken);

    private async Task AppendReplyAsync(TicketDto ticket, TicketActorRef by, string html, DateTimeOffset at, bool isNote, CancellationToken cancellationToken)
    {
        // cleanHTML(F.html): the timeline renders this as raw markup, so sanitize before storing.
        if (html.Length > TicketHtmlSanitizer.MaxLength) throw new ArgumentException("The message is too long.", nameof(html));
        html = TicketHtmlSanitizer.Sanitize(html);

        var entry = await _ticketRepository.AppendTimelineEntryAsync(new TicketTimelineEntryDto
        {
            Id = Guid.NewGuid().ToString("n"),
            TicketId = ticket.Id,
            Kind = isNote ? TicketTimelineEntryKind.Note : TicketTimelineEntryKind.Reply,
            At = at,
            By = by,
            Html = html
        }, cancellationToken);

        ticket.Timeline.Add(entry);
        ticket.Updated = at;
    }

    /// <summary>
    /// Enum.Parse also accepts numbers ("7"), comma lists and padding, and the raw string used to be
    /// stored -- only an exact defined name is allowed. Throws ArgumentException (a 400 in the Api).
    /// </summary>
    private static T ParseDefined<T>(string value, string paramName) where T : struct, Enum
        => Enum.GetNames<T>().Contains(value) ? Enum.Parse<T>(value)
            : throw new ArgumentException($"Unknown {paramName} '{value}'.", paramName);

    private async Task AppendEventAsync(TicketDto ticket, TicketActorRef by, string text, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var entry = await _ticketRepository.AppendTimelineEntryAsync(new TicketTimelineEntryDto
        {
            Id = Guid.NewGuid().ToString("n"),
            TicketId = ticket.Id,
            Kind = TicketTimelineEntryKind.Event,
            At = at,
            By = by,
            Text = text
        }, cancellationToken);

        ticket.Timeline.Add(entry);
        ticket.Updated = at;
    }

    private async Task<string> ShortNameForActorAsync(TicketActorRef actor, CancellationToken cancellationToken)
    {
        if (actor.Type == "system" || actor.Id == null) return "System";

        var name = actor.Type == "agent"
            ? (await _agentRepository.GetByIdAsync(actor.Id, cancellationToken))?.Name
            : (await _userRepository.GetByIdAsync(actor.Id, cancellationToken))?.Name;

        return ShortName(name);
    }

    /// <summary>Ports `short(n)` from topsupport-app.js: "Jamie Moore" -> "J. Moore", single-word names pass through unchanged.</summary>
    private static string ShortName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var parts = name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? $"{parts[0][0]}. {parts[^1]}" : parts[0];
    }

    /// <summary>Renders a TicketStatus the way the demo's STATUSES strings read ("In Progress", not "InProgress").</summary>
    private static string FormatStatus(TicketStatus status) => status switch
    {
        TicketStatus.InProgress => "In Progress",
        TicketStatus.AwaitingUser => "Awaiting User",
        _ => status.ToString()
    };

    private static DateOnly? ParseDateOnly(string? value) =>
        !string.IsNullOrEmpty(value) && DateOnly.TryParse(value, out var date) ? date : null;
}
