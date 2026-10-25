using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Services;
using TopSupport.Api.Auth;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Ticket endpoints: CRUD/reply/status/priority/assign/bulk actions, topic catalog +
/// field-schema, queue search, task add/set-done, attachment list, nav-counts. Attachment
/// content download lives on AttachmentsController; canned-response CRUD/preview lives on
/// CannedResponsesController. No merge endpoint — ticket merge is out of scope.
/// <para>
/// Read/create/user-reply/attachment/topic/field-schema/nav-count endpoints are open to any
/// signed-in account, with end users limited to their own tickets (404 for anyone else's) and
/// never shown internal notes; everything else is agents and admins only. The acting account
/// always comes from the token; no request carries one.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly TicketFieldOptionsResolver _optionsResolver;
    private readonly QueueService _queueService;
    private readonly TicketAttachmentService _attachments;

    public TicketsController(TicketService ticketService, TicketFieldOptionsResolver optionsResolver, QueueService queueService, TicketAttachmentService attachments)
    {
        _ticketService = ticketService;
        _optionsResolver = optionsResolver;
        _queueService = queueService;
        _attachments = attachments;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null || (User.IsEndUser() && !User.IsOwnTicket(ticket))) return NotFound();
        return Ok(User.ForCaller(ticket));
    }

    /// <summary>"My Tickets": every ticket where the user is requester or affected. End users only for themselves.</summary>
    [HttpGet("by-requester/{requesterUserId}")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetByRequester(string requesterUserId, CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && requesterUserId != User.AccountId()) return Forbid();

        return Ok(await _ticketService.GetMyTicketsAsync(requesterUserId, cancellationToken));
    }

    [HttpGet("topics")]
    public ActionResult<IReadOnlyList<TicketTopic>> GetTopics() => Ok(TicketTopics.TOPICS);

    /// <summary>
    /// End users only get asset-backed options for themselves: for any other affectedUserId (the
    /// Raise form's "affected user is not me" path) the schema is resolved without an affected user,
    /// so another person's assets are never listed.
    /// </summary>
    [HttpGet("field-schema/{topic}")]
    public async Task<ActionResult<List<TicketFieldDescriptor>>> GetFieldSchema(
        string topic, [FromQuery] string? affectedUserId, CancellationToken cancellationToken)
    {
        if (TicketTopics.Find(topic) is null) return NotFound();
        if (User.IsEndUser() && affectedUserId != User.AccountId()) affectedUserId = null;

        return Ok(await _optionsResolver.ResolveAsync(topic, affectedUserId, new Dictionary<string, object?>(), cancellationToken));
    }

    /// <summary>End users may only raise tickets as the requester themselves; agents may raise on behalf of anyone.</summary>
    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && request.RequesterUserId != User.AccountId()) return Forbid();

        var ticket = await _ticketService.CreateTicketAsync(request, User.Actor(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, User.ForCaller(ticket));
    }

    /// <summary>End users only, on their own tickets; agents reply through replies/agent.</summary>
    [HttpPost("{id}/replies/user")]
    public async Task<ActionResult<TicketDto>> AddUserReply(string id, AddUserReplyRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Forbid();
        if (await _ticketService.GetTicketAsync(id, cancellationToken) is not { } existing || !User.IsOwnTicket(existing)) return NotFound();

        return Ok(User.ForCaller(await _ticketService.AddUserReplyAsync(id, User.AccountId(), request, cancellationToken)));
    }

    [HttpPost("{id}/replies/agent")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> AddAgentReply(string id, AddAgentReplyRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.AddAgentReplyAsync(id, User.AccountId(), request, cancellationToken));

    public record ChangeStatusRequest(string Status);

    [HttpPut("{id}/status")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeStatus(string id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.ChangeStatusAsync(id, request.Status, User.Actor(), cancellationToken));

    public record ChangePriorityRequest(string Priority);

    [HttpPut("{id}/priority")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangePriority(string id, ChangePriorityRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.ChangePriorityAsync(id, request.Priority, User.Actor(), cancellationToken));

    public record ChangeTopicRequest(string Topic);

    [HttpPut("{id}/topic")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeTopic(string id, ChangeTopicRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.ChangeTopicAsync(id, request.Topic, User.Actor(), cancellationToken));

    public record ChangePropertiesRequest(string Status, string Priority, string Topic);

    [HttpPut("{id}/properties")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeProperties(string id, ChangePropertiesRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.ChangePropertiesAsync(id, request.Status, request.Priority, request.Topic, User.Actor(), cancellationToken));

    [HttpPut("{id}/assign")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> Assign(string id, AssignTicketRequest request, CancellationToken cancellationToken)
        => Ok(request.Kind switch
        {
            "agent" => await _ticketService.AssignToAgentAsync(id, request.Id, User.Actor(), cancellationToken),
            "team" => await _ticketService.AssignToTeamAsync(id, request.Id, User.Actor(), cancellationToken),
            _ => throw new ValidationException("kind", "Choose an agent or a team.")
        });

    /// <summary>The signed-in agent claims the ticket.</summary>
    [HttpPost("{id}/claim")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> Claim(string id, CancellationToken cancellationToken)
        => Ok(await _ticketService.ClaimAsync(id, User.AccountId(), cancellationToken));

    // TicketService validates the whole selection (<= 100 distinct, existing ids) before changing any.

    [HttpPost("bulk/status")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkStatus(BulkStatusRequest request, CancellationToken cancellationToken)
    {
        await _ticketService.BulkChangeStatusAsync(request.TicketIds, request.Status, User.Actor(), cancellationToken);
        return Ok();
    }

    [HttpPost("bulk/priority")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkPriority(BulkPriorityRequest request, CancellationToken cancellationToken)
    {
        await _ticketService.BulkChangePriorityAsync(request.TicketIds, request.Priority, User.Actor(), cancellationToken);
        return Ok();
    }

    [HttpPost("bulk/assign")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkAssign(BulkAssignRequest request, CancellationToken cancellationToken)
    {
        await _ticketService.BulkAssignAsync(request.TicketIds, request.Kind, request.Id, User.Actor(), cancellationToken);
        return Ok();
    }

    [HttpPost("bulk/close")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkClose(BulkCloseRequest request, CancellationToken cancellationToken)
    {
        var closed = await _ticketService.BulkCloseAsync(request.TicketIds, request.Status, request.Note, User.Actor(), cancellationToken);
        return Ok(new { closed });
    }

    /// <summary>Unknown ticket -> 404; blank/over-long text, unknown/inactive assignee or an out-of-range due date -> 400.</summary>
    public record AddTaskRequest(string Text, string AssigneeAgentId, DateOnly? Due);

    [HttpPost("{id}/tasks")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketTaskDto>> AddTask(string id, AddTaskRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.AddTaskAsync(id, request.Text, request.AssigneeAgentId, request.Due, User.Actor(), cancellationToken));

    public record SetTaskDoneRequest(bool Done);

    /// <summary>Scoped to its ticket: a task id that isn't on ticket {id} (or an unknown ticket) is a 404.</summary>
    [HttpPut("{id}/tasks/{taskId}/done")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketTaskDto>> SetTaskDone(string id, string taskId, SetTaskDoneRequest request, CancellationToken cancellationToken)
        => Ok(await _ticketService.SetTaskDoneAsync(id, taskId, request.Done, User.Actor(), cancellationToken));

    [HttpGet("{id}/attachments")]
    public async Task<ActionResult<IReadOnlyList<TicketAttachmentDto>>> GetAttachments(string id, CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Ok(await _attachments.GetForTicketAsync(id, cancellationToken));

        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null || !User.IsOwnTicket(ticket)) return NotFound();
        return Ok(EndUserTicketAccess.VisibleAttachments(ticket, await _attachments.GetForTicketAsync(id, cancellationToken)));
    }

    /// <summary>
    /// The uploader is the signed-in account. End users only on their own tickets (404 otherwise).
    /// The rules (ticket open, allow-listed type, size and per-ticket caps, anchor entry) live in
    /// TicketAttachmentService.AddAsync; a rejected file is a 400. The client's content type is ignored.
    /// </summary>
    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<TicketAttachmentDto>> UploadAttachment(
        string id, IFormFile? file, [FromForm] string? timelineEntryId, CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && (await _ticketService.GetTicketAsync(id, cancellationToken) is not { } ticket || !User.IsOwnTicket(ticket)))
        {
            return NotFound();
        }

        if (file is null) throw new ValidationException("file", "Choose a file to attach.");
        if (file.Length > TicketAttachmentRules.MaxFileSizeBytes) throw new ValidationException("file", TicketAttachmentRules.TooLargeMessage);

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return Ok(await _attachments.AddAsync(id, timelineEntryId, file.FileName, stream.ToArray(), User.Actor(), cancellationToken));
    }

    /// <summary>The "mine" queue and the "assigned to me" filter are the signed-in agent's.</summary>
    [HttpGet("queue")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketQueuePageResult>> Queue([FromQuery] TicketQueueRequest request, CancellationToken cancellationToken)
        => Ok(await _queueService.SearchAsync(request, User.AccountId(), cancellationToken));

    /// <summary>
    /// Sidebar badges for the signed-in account. End users get only their own MyOpenTickets count,
    /// agents the queue counts.
    /// </summary>
    [HttpGet("nav-counts")]
    public async Task<ActionResult<TicketNavCountsDto>> NavCounts(CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Ok(await _queueService.GetNavCountsAsync(null, User.AccountId(), cancellationToken));

        var counts = await _queueService.GetNavCountsAsync(User.AccountId(), null, cancellationToken);
        return Ok(new TicketNavCountsDto { MyOpenTickets = counts.MyOpenTickets });
    }
}
