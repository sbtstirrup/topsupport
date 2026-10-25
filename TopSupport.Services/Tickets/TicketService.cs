using TopSupport.Models.Admin;
using TopSupport.Models.Enums;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Services.Tickets;

/// <summary>
/// The ticket lifecycle, porting createTicket/changeStatus/assignTicket/
/// addEntry/addEvent and the ACT['user-reply']/ACT['agent-reply'] handlers from
/// topsupport-app.js (lines ~404-439, ~1343-1352, ~1591-1600) one layer above raw data
/// access. No merge method anywhere — ticket merge is explicitly out of scope.
/// Each mutation also writes the same audit(...) entry the demo does (category Ticket),
/// attributed to the acting account on the request.
/// </summary>
public class TicketService
{
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
    private readonly TicketFormValidator _validator;
    private readonly AuditService _audit;
    private readonly TimeProvider _time;

    public TicketService(
        ITicketRepository ticketRepository,
        ITeamRepository teamRepository,
        IAgentRepository agentRepository,
        IUserRepository userRepository,
        TicketFormValidator validator,
        AuditService audit,
        TimeProvider time)
    {
        _ticketRepository = ticketRepository;
        _teamRepository = teamRepository;
        _agentRepository = agentRepository;
        _userRepository = userRepository;
        _validator = validator;
        _audit = audit;
        _time = time;
    }

    public Task<TicketDto?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
        => _ticketRepository.GetByIdAsync(ticketId, cancellationToken);

    /// <summary>"My Tickets": every ticket where the user is requester or affected.</summary>
    public Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(string requesterUserId, CancellationToken cancellationToken = default)
        => _ticketRepository.GetByRequesterOrAffectedAsync(requesterUserId, cancellationToken);

    /// <summary>
    /// Validates via TicketFormValidator first (agentMode = createdBy.Type == TicketActorTypes.Agent); on
    /// failure throws ValidationException. Numbers via usp_Ticket_NextNumber (zero-padded
    /// to 6 digits, "T" prefix), assigns the department's default team, leaves agent null,
    /// forces status Open, derives priority from IMPACT_PRI when the topic has an impact
    /// field, and seeds the "Created by"/"Assigned to team" timeline events (plus the leaver
    /// checklist event + 4 tasks for topic == "leaver") exactly as createTicket() does.
    /// </summary>
    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, TicketActorRef createdBy, CancellationToken cancellationToken = default)
    {
        var agentMode = createdBy.Type == TicketActorTypes.Agent;
        request.Fields ??= new Dictionary<string, object?>();
        var validation = await _validator.ValidateAsync(request, agentMode, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var topicDef = TicketTopics.Find(request.Topic)
            ?? throw new InvalidOperationException($"Topic '{request.Topic}' passed validation but is unknown.");

        // Everything derived from the answers is worked out before the first insert, so a bad
        // value can't leave a half-created ticket. Only answers to fields currently shown are kept.
        var storedFields = TicketFormValidator.VisibleFields(TicketTopicFieldSchemas.GetFields(request.Topic), request.Fields)
            .Where(f => request.Fields.ContainsKey(f.Key))
            .ToDictionary(f => f.Key, f => request.Fields[f.Key]);

        DateOnly? leaverDue = null;
        if (request.Topic == "leaver")
        {
            // leavingDate is required for leavers, so the validator has already checked it parses.
            if (!TicketFormValidator.TryParseFieldDate(storedFields.GetValueOrDefault("leavingDate"), out var leavingDate))
            {
                throw new InvalidOperationException($"Ticket leavingDate '{storedFields.GetValueOrDefault("leavingDate")}' passed validation but doesn't parse.");
            }
            leaverDue = leavingDate;
        }

        var now = _time.GetUtcNow();
        var nextNumber = await _ticketRepository.GetNextNumberAsync(cancellationToken);
        var number = nextNumber.ToString("D6");
        var id = "T" + number;

        var priority = TicketPriority.Normal;
        if (storedFields.TryGetValue("impact", out var impactValue) &&
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
            Subject = request.Subject.Trim(),
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
            Fields = storedFields
        };

        await _ticketRepository.CreateAsync(ticket, cancellationToken);

        var requesterShortName = await ShortNameForActorAsync(new TicketActorRef { Type = TicketActorTypes.User, Id = request.RequesterUserId }, cancellationToken);
        var creatorShortName = agentMode ? await ShortNameForActorAsync(createdBy, cancellationToken) : requesterShortName;

        var createdText = agentMode
            ? $"Created by {creatorShortName} on behalf of {requesterShortName}"
            : $"Created by {requesterShortName}";

        await AppendEventAsync(ticket, createdBy, createdText, now, cancellationToken);
        await AppendEventAsync(ticket, TicketActorRef_System, $"Assigned to {defaultTeam.Name}", now, cancellationToken);

        if (request.Topic == "leaver")
        {
            foreach (var (text, assigneeAgentId) in LeaverTasks)
            {
                var task = await _ticketRepository.InsertTaskAsync(new TicketTaskDto
                {
                    Id = Guid.NewGuid().ToString("n"),
                    TicketId = ticket.Id,
                    Text = text,
                    AssigneeAgentId = assigneeAgentId,
                    Due = leaverDue,
                    Done = false
                }, cancellationToken);
                ticket.Tasks.Add(task);
            }

            await AppendEventAsync(ticket, TicketActorRef_System, "Leaver checklist added (4 tasks)", now, cancellationToken);
        }

        await AuditAsync(createdBy, "Created ticket", ticket, cancellationToken);
        return ticket;
    }

    /// <summary>
    /// Throws TicketClosedException if the ticket is Closed. Posts the reply, then reopens
    /// with the demo's exact (intentionally asymmetric) wording: Resolved -> "Reopened by X",
    /// AwaitingUser -> "Status changed from Awaiting User to Open by X". Any other status is
    /// left unchanged.
    /// </summary>
    public async Task<TicketDto> AddUserReplyAsync(string ticketId, string userId, AddUserReplyRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        if (ticket.Status == TicketStatus.Closed)
        {
            throw new ValidationException("", "This ticket is closed. You can't reply to it.");
        }

        var by = new TicketActorRef { Type = TicketActorTypes.User, Id = userId };
        var now = _time.GetUtcNow();

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

    /// <summary>
    /// Posts a public reply or internal note. For a public reply with StatusAfterSending set,
    /// changes status quietly (no extra audit-style event beyond the status-change event
    /// itself) afterward, matching ACT['agent-reply'].
    /// </summary>
    public async Task<TicketDto> AddAgentReplyAsync(string ticketId, string agentId, AddAgentReplyRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        if (ticket.Status == TicketStatus.Closed)
        {
            // Same rule as the user reply: Closed is final, so no replies or notes (the agent view
            // hides the reply box -- "Reopen it to reply or add notes." -- this enforces it).
            throw new ValidationException("", "This ticket is closed. Reopen it to reply or add notes.");
        }

        // Validate "status after sending" before anything is written.
        var statusAfter = !request.IsNote && !string.IsNullOrEmpty(request.StatusAfterSending)
            ? ParseDefined<TicketStatus>(request.StatusAfterSending, "status")
            : (TicketStatus?)null;

        var by = new TicketActorRef { Type = TicketActorTypes.Agent, Id = agentId };
        var now = _time.GetUtcNow();

        await AppendReplyAsync(ticket, by, request.Html, now, request.IsNote, cancellationToken);

        if (statusAfter is { } next)
        {
            // Not quiet: the status change gets its own "Changed status to ..." audit entry.
            await ChangeStatusInternalAsync(ticket, next.ToString(), by, now, quiet: false, cancellationToken);
        }

        await AuditAsync(by, request.IsNote ? "Added internal note" : "Sent public reply", ticket, cancellationToken);
        return ticket;
    }

    /// <summary>
    /// No-op if the status is unchanged. Event wording branches exactly as changeStatus(t, s):
    /// closing (Resolved/Closed) -> "Closed by X with status {s}"; reopening from Resolved/
    /// Closed -> "Reopened by X"; otherwise -> "Status changed from {prev} to {s} by X".
    /// </summary>
    public async Task<TicketDto> ChangeStatusAsync(string ticketId, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        await ChangeStatusInternalAsync(ticket, status, changedBy, _time.GetUtcNow(), quiet: false, cancellationToken);
        return ticket;
    }

    public async Task<TicketDto> ChangePriorityAsync(string ticketId, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var now = _time.GetUtcNow();
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
        var topicDef = TicketTopics.Find(topic) ?? throw new ValidationException("topic", $"Unknown topic '{topic}'.");
        if (topic == ticket.Topic) return ticket;

        var now = _time.GetUtcNow();
        var prevLabel = TicketTopics.Find(ticket.Topic)?.Label ?? ticket.Topic;

        ticket.Topic = topic;
        ticket.Department = topicDef.Department;
        await _ticketRepository.UpdateTopicAsync(ticketId, topic, topicDef.Department, now, cancellationToken);

        // Same timeline wording as ACT['change-go'].
        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        await AppendEventAsync(ticket, changedBy, $"Help topic changed from {prevLabel} to {topicDef.Label} by {shortName}", now, cancellationToken);
        await AuditAsync(changedBy, $"Changed help topic to {topicDef.Label}", ticket, cancellationToken);

        return ticket;
    }

    /// <summary>
    /// The agent "Change" modal (ACT['change-go']): applies whichever of priority/topic/status
    /// differ (status quietly), each with its own timeline event, then writes ONE
    /// "Changed ticket properties" audit entry if anything changed.
    /// </summary>
    public async Task<TicketDto> ChangePropertiesAsync(string ticketId, string status, string priority, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);

        // Validate all three before writing anything, so a bad value can't leave a half-applied change.
        var nextPriority = ParseDefined<TicketPriority>(priority, nameof(priority));
        var nextStatus = ParseDefined<TicketStatus>(status, nameof(status));
        var topicDef = TicketTopics.Find(topic) ?? throw new ValidationException("topic", $"Unknown topic '{topic}'.");

        var now = _time.GetUtcNow();
        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        var changed = 0;

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
            var prevLabel = TicketTopics.Find(ticket.Topic)?.Label ?? ticket.Topic;
            ticket.Topic = topic;
            ticket.Department = topicDef.Department;
            await _ticketRepository.UpdateTopicAsync(ticketId, topic, topicDef.Department, now, cancellationToken);
            await AppendEventAsync(ticket, changedBy, $"Help topic changed from {prevLabel} to {topicDef.Label} by {shortName}", now, cancellationToken);
            changed++;
        }

        if (nextStatus != ticket.Status)
        {
            await ChangeStatusInternalAsync(ticket, nextStatus.ToString(), changedBy, now, quiet: true, cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            await AuditAsync(changedBy, "Changed ticket properties", ticket, cancellationToken);
        }

        return ticket;
    }

    /// <summary>Defaults the ticket's team to the agent's first team if it has none yet (assignTicket's un-claim-safe default).</summary>
    public async Task<TicketDto> AssignToAgentAsync(string ticketId, string agentId, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var agent = await RequireActiveAgentAsync(agentId, cancellationToken);
        await AssignToAgentInternalAsync(ticket, agent, assignedBy, _time.GetUtcNow(), cancellationToken);
        return ticket;
    }

    /// <summary>Clears any individually-assigned agent (assignTicket's team-assign un-claim behavior).</summary>
    public async Task<TicketDto> AssignToTeamAsync(string ticketId, string teamId, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var team = await RequireTeamAsync(teamId, cancellationToken);
        await AssignToTeamInternalAsync(ticket, team, assignedBy, _time.GetUtcNow(), cancellationToken);
        return ticket;
    }

    /// <summary>
    /// ACT['t-claim']: the demo lets an agent claim any ticket that isn't already theirs (the
    /// Claim button only hides when it's assigned to them), so the rule is kept -- but the write is
    /// a compare-and-set on the assignee the agent saw, so of two simultaneous claims only one
    /// succeeds and the other gets TicketConflictException (409) instead of silently overwriting.
    /// </summary>
    public async Task<TicketDto> ClaimAsync(string ticketId, string agentId, CancellationToken cancellationToken = default)
    {
        var by = new TicketActorRef { Type = TicketActorTypes.Agent, Id = agentId };
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var agent = await RequireActiveAgentAsync(agentId, cancellationToken);
        if (ticket.AssignedAgentId == agentId) return ticket;

        var now = _time.GetUtcNow();
        var teamId = ticket.AssignedTeamId ?? (agent.TeamIds.Count > 0 ? agent.TeamIds[0] : null);
        if (!await _ticketRepository.ClaimIfUnchangedAsync(ticket.Id, agentId, teamId, ticket.AssignedAgentId, now, cancellationToken))
        {
            throw new TicketConflictException("Someone else changed who this ticket is assigned to. Refresh the ticket and try again.");
        }

        ticket.AssignedAgentId = agentId;
        ticket.AssignedTeamId = teamId;
        await AppendEventAsync(ticket, by, $"Assigned to {ShortName(agent.Name)}", now, cancellationToken);
        await AuditAsync(by, $"Assigned ticket to {ShortName(agent.Name)}", ticket, cancellationToken);

        if (ticket.Status == TicketStatus.Open)
        {
            // changeStatus(t, 'In Progress', true) in the demo adds the timeline event; the status
            // flip is also audited here so every status change has an audit entry.
            await ChangeStatusInternalAsync(ticket, nameof(TicketStatus.InProgress), by, now, quiet: false, cancellationToken);
        }

        return ticket;
    }

    public async Task BulkChangeStatusAsync(IEnumerable<string> ticketIds, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var next = ParseDefined<TicketStatus>(status, nameof(status));
        var tickets = await RequireTicketsAsync(ticketIds, cancellationToken);
        var now = _time.GetUtcNow();
        foreach (var ticket in tickets)
        {
            await ChangeStatusInternalAsync(ticket, next.ToString(), changedBy, now, quiet: false, cancellationToken);
        }
    }

    public async Task BulkChangePriorityAsync(IEnumerable<string> ticketIds, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        ParseDefined<TicketPriority>(priority, nameof(priority));
        var tickets = await RequireTicketsAsync(ticketIds, cancellationToken);
        foreach (var ticket in tickets)
        {
            await ChangePriorityAsync(ticket.Id, priority, changedBy, cancellationToken);
        }
    }

    public async Task BulkAssignAsync(IEnumerable<string> ticketIds, string kind, string id, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        // Resolve the assignee and every ticket before changing any, so nothing is half-applied.
        var agent = kind == "agent" ? await RequireActiveAgentAsync(id, cancellationToken) : null;
        var team = kind == "team" ? await RequireTeamAsync(id, cancellationToken) : null;
        if (agent is null && team is null) throw new ValidationException("kind", "Choose an agent or a team.");

        var tickets = await RequireTicketsAsync(ticketIds, cancellationToken);
        var now = _time.GetUtcNow();
        foreach (var ticket in tickets)
        {
            if (agent is not null)
            {
                await AssignToAgentInternalAsync(ticket, agent, assignedBy, now, cancellationToken);
            }
            else
            {
                await AssignToTeamInternalAsync(ticket, team!, assignedBy, now, cancellationToken);
            }
        }
    }

    /// <summary>
    /// ACT['close-go']: optional internal note, then changeStatus(t, Resolved|Closed). Unlike the
    /// demo, tickets that are already Closed (final) or already at the chosen status are skipped --
    /// otherwise a bulk "Resolved" would turn a final Closed ticket back into a reopenable one, and
    /// the note would land on tickets that weren't closed by this action.
    /// </summary>
    public async Task<int> BulkCloseAsync(IEnumerable<string> ticketIds, string status, string? note, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var next = ParseDefined<TicketStatus>(status, nameof(status));
        if (next is not (TicketStatus.Resolved or TicketStatus.Closed))
        {
            throw new ValidationException("status", "Close with status Resolved or Closed.");
        }

        note = note?.Trim();
        if (FieldLimits.TooLong(note, BulkCloseRequest.MaxNoteLength) is { } noteTooLong) throw new ValidationException("note", noteTooLong);

        var tickets = await RequireTicketsAsync(ticketIds, cancellationToken);
        var now = _time.GetUtcNow();
        var closed = 0;
        foreach (var ticket in tickets)
        {
            if (ticket.Status == TicketStatus.Closed || ticket.Status == next) continue;

            if (!string.IsNullOrEmpty(note))
            {
                await AppendReplyAsync(ticket, changedBy, TextToHtml(note), now, isNote: true, cancellationToken);
            }

            await ChangeStatusInternalAsync(ticket, next.ToString(), changedBy, now, quiet: false, cancellationToken);
            closed++;
        }

        return closed;
    }

    public async Task<TicketTaskDto> AddTaskAsync(string ticketId, string text, string assigneeAgentId, DateOnly? due, TicketActorRef addedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);

        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) throw new ValidationException("text", "Enter the task.");
        if (FieldLimits.TooLong(text, FieldLimits.TaskText) is { } textTooLong) throw new ValidationException("text", textTooLong);

        var assignee = await RequireActiveAgentAsync(assigneeAgentId, cancellationToken);

        if (due is { } dueDate)
        {
            var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
            if (dueDate < today.AddYears(-1) || dueDate > today.AddYears(5))
            {
                throw new ValidationException("due", "Choose a due date within the last year or the next five years.");
            }
        }

        var task = await _ticketRepository.InsertTaskAsync(new TicketTaskDto
        {
            Id = Guid.NewGuid().ToString("n"),
            TicketId = ticket.Id,
            Text = text,
            AssigneeAgentId = assignee.Id,
            Due = due,
            Done = false
        }, cancellationToken);
        ticket.Tasks.Add(task);

        var now = _time.GetUtcNow();
        var shortName = await ShortNameForActorAsync(addedBy, cancellationToken);
        await AppendEventAsync(ticket, addedBy, $"Task added by {shortName}: {text} (assigned to {ShortName(assignee.Name)})", now, cancellationToken);
        await AuditAsync(addedBy, "Added task", ticket, cancellationToken);

        return task;
    }

    /// <summary>Throws KeyNotFoundException when the ticket, or that task on it, doesn't exist.</summary>
    public async Task<TicketTaskDto> SetTaskDoneAsync(string ticketId, string taskId, bool done, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var ticket = await RequireTicketAsync(ticketId, cancellationToken);
        var task = ticket.Tasks.FirstOrDefault(k => k.Id == taskId)
            ?? throw new KeyNotFoundException($"Task '{taskId}' was not found on ticket '{ticketId}'.");
        if (task.Done == done) return task;

        await _ticketRepository.SetTaskDoneAsync(task.Id, done, cancellationToken);
        task.Done = done;

        var now = _time.GetUtcNow();
        var shortName = await ShortNameForActorAsync(changedBy, cancellationToken);
        await AppendEventAsync(ticket, changedBy, $"Task {(done ? "completed" : "reopened")} by {shortName}: {task.Text}", now, cancellationToken);
        await AuditAsync(changedBy, done ? "Completed task" : "Reopened task", ticket, cancellationToken);

        return task;
    }

    // ---------- internal helpers ----------

    private static readonly TicketActorRef TicketActorRef_System = new() { Type = TicketActorTypes.System };

    private async Task<TicketDto> RequireTicketAsync(string ticketId, CancellationToken cancellationToken)
        => await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket '{ticketId}' was not found.");

    /// <summary>
    /// Bulk actions: de-duplicates the ids, rejects an empty/oversized selection or blank id (400)
    /// and loads every ticket up front -- any unknown id is a 404 before anything is changed.
    /// </summary>
    private async Task<List<TicketDto>> RequireTicketsAsync(IEnumerable<string>? ticketIds, CancellationToken cancellationToken)
    {
        if (ticketIds is null) throw new ValidationException("ticketIds", "Choose at least one ticket.");

        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ticketIds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ValidationException("ticketIds", "Every selected ticket needs an id.");
            if (seen.Add(id)) ids.Add(id);
            if (ids.Count > BulkCloseRequest.MaxTickets)
            {
                throw new ValidationException("ticketIds", $"Choose {BulkCloseRequest.MaxTickets} tickets or fewer.");
            }
        }

        if (ids.Count == 0) throw new ValidationException("ticketIds", "Choose at least one ticket.");

        var tickets = new List<TicketDto>(ids.Count);
        foreach (var id in ids)
        {
            tickets.Add(await RequireTicketAsync(id, cancellationToken));
        }
        return tickets;
    }

    /// <summary>Unknown, inactive or locked agents can't be given work (assign, claim, task assignee): 400.</summary>
    private async Task<AgentDto> RequireActiveAgentAsync(string? agentId, CancellationToken cancellationToken)
    {
        var agent = string.IsNullOrWhiteSpace(agentId) ? null : await _agentRepository.GetByIdAsync(agentId, cancellationToken);
        return agent is { Active: true, Locked: false }
            ? agent
            : throw new ValidationException("agentId", "Choose an active agent.");
    }

    private async Task<TeamDto> RequireTeamAsync(string? teamId, CancellationToken cancellationToken)
        => (string.IsNullOrWhiteSpace(teamId) ? null : await _teamRepository.GetByIdAsync(teamId, cancellationToken))
            ?? throw new ValidationException("teamId", "Choose a team.");

    /// <summary>assignTicket(t, 'agent', id).</summary>
    private async Task AssignToAgentInternalAsync(TicketDto ticket, AgentDto agent, TicketActorRef assignedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ticket.AssignedAgentId = agent.Id;
        if (ticket.AssignedTeamId == null && agent.TeamIds.Count > 0)
        {
            ticket.AssignedTeamId = agent.TeamIds[0];
        }

        await _ticketRepository.AssignAsync(ticket.Id, ticket.AssignedAgentId, ticket.AssignedTeamId, now, cancellationToken);
        await AppendEventAsync(ticket, assignedBy, $"Assigned to {ShortName(agent.Name)}", now, cancellationToken);
        await AuditAsync(assignedBy, $"Assigned ticket to {ShortName(agent.Name)}", ticket, cancellationToken);
    }

    /// <summary>assignTicket(t, 'team', id): assigning to a team clears the individual agent.</summary>
    private async Task AssignToTeamInternalAsync(TicketDto ticket, TeamDto team, TicketActorRef assignedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ticket.AssignedTeamId = team.Id;
        ticket.AssignedAgentId = null;

        await _ticketRepository.AssignAsync(ticket.Id, null, team.Id, now, cancellationToken);
        await AppendEventAsync(ticket, assignedBy, $"Assigned to {team.Name}", now, cancellationToken);
        await AuditAsync(assignedBy, $"Assigned ticket to {team.Name}", ticket, cancellationToken);
    }

    /// <summary>textToHTML(s): HTML-encodes plain text; blank lines become paragraphs, single newlines &lt;br&gt;.</summary>
    private static string TextToHtml(string text)
        => string.Concat(System.Text.RegularExpressions.Regex.Split(System.Net.WebUtility.HtmlEncode(text).Replace("\r\n", "\n"), @"\n{2,}")
            .Select(p => "<p>" + p.Replace("\n", "<br>") + "</p>"));

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
            text = $"Closed by {shortName} with status {TicketStatusText.Label(next)}";
        }
        else if (prev is TicketStatus.Resolved or TicketStatus.Closed)
        {
            text = $"Reopened by {shortName}";
        }
        else
        {
            text = $"Status changed from {TicketStatusText.Label(prev)} to {TicketStatusText.Label(next)} by {shortName}";
        }

        await AppendEventAsync(ticket, changedBy, text, now, cancellationToken);

        if (!quiet)
        {
            await AuditAsync(changedBy, $"Changed status to {TicketStatusText.Label(next)}", ticket, cancellationToken);
        }
    }

    /// <summary>audit(action, '#' + t.num, 'Ticket'), attributed to the acting account ("system" when there is none).</summary>
    private Task AuditAsync(TicketActorRef actor, string action, TicketDto ticket, CancellationToken cancellationToken)
        => _audit.LogAsync(actor.Type == TicketActorTypes.System || actor.Id == null ? TicketActorTypes.System : actor.Id, action, "#" + ticket.Number, AuditCategories.Ticket, cancellationToken);

    private async Task AppendReplyAsync(TicketDto ticket, TicketActorRef by, string html, DateTimeOffset at, bool isNote, CancellationToken cancellationToken)
    {
        // cleanHTML(F.html): the timeline renders this as raw markup, so sanitize before storing.
        if (html.Length > TicketHtmlSanitizer.MaxLength) throw new ValidationException("html", "The message is too long.");
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
            : throw new ValidationException(paramName, $"Unknown {paramName} '{value}'.");

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
        if (actor.Type == TicketActorTypes.System || actor.Id == null) return "System";

        // The actor comes from the signed-in account or a validated id, so a missing one is a bug:
        // fail rather than write "Closed by  with status ..." into the timeline.
        var name = actor.Type == TicketActorTypes.Agent
            ? (await _agentRepository.GetByIdAsync(actor.Id, cancellationToken))?.Name
            : (await _userRepository.GetByIdAsync(actor.Id, cancellationToken))?.Name;

        return ShortName(name ?? throw new InvalidOperationException($"No {actor.Type} with id '{actor.Id}' for a ticket timeline entry."));
    }

    /// <summary>Ports `short(n)` from topsupport-app.js: "Jamie Moore" -> "J. Moore", single-word names pass through unchanged.</summary>
    private static string ShortName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var parts = name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? $"{parts[0][0]}. {parts[^1]}" : parts[0];
    }
}
