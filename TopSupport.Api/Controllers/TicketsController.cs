using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
/// never shown internal notes; everything else is agents and admins only. Every actor
/// (ChangedBy, AssignedBy, AgentId, UserId, CreatedBy, uploadedBy...) comes from the token —
/// the matching request fields are still accepted but ignored.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketFieldOptionsResolver _optionsResolver;
    private readonly IQueueService _queueService;

    public TicketsController(ITicketService ticketService, ITicketFieldOptionsResolver optionsResolver, IQueueService queueService)
    {
        _ticketService = ticketService;
        _optionsResolver = optionsResolver;
        _queueService = queueService;
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

        var tickets = await _ticketService.GetMyTicketsAsync(requesterUserId, cancellationToken);
        return Ok(tickets);
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

        var fields = await _optionsResolver.ResolveAsync(topic, affectedUserId, new Dictionary<string, object?>(), cancellationToken);
        return Ok(fields);
    }

    /// <summary>CreatedBy is ignored (the creator is the signed-in account).</summary>
    public record CreateTicketApiRequest(CreateTicketRequest Request, TicketActorRef CreatedBy);

    /// <summary>End users may only raise tickets as the requester themselves; agents may raise on behalf of anyone.</summary>
    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketApiRequest body, CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && body.Request.RequesterUserId != User.AccountId()) return Forbid();

        try
        {
            var ticket = await _ticketService.CreateTicketAsync(body.Request, User.Actor(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, User.ForCaller(ticket));
        }
        catch (TicketValidationException ex)
        {
            return ValidationProblem(ToModelState(ex.ValidationResult));
        }
    }

    private static ModelStateDictionary ToModelState(TicketFormValidationResult result)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (key, message) in result.Errors)
        {
            modelState.AddModelError(key, message);
        }
        return modelState;
    }

    /// <summary>End users only, on their own tickets; agents reply through replies/agent.</summary>
    [HttpPost("{id}/replies/user")]
    public async Task<ActionResult<TicketDto>> AddUserReply(string id, AddUserReplyRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Forbid();
        if (await _ticketService.GetTicketAsync(id, cancellationToken) is not { } existing || !User.IsOwnTicket(existing)) return NotFound();

        request.UserId = User.AccountId();
        try
        {
            return Ok(User.ForCaller(await _ticketService.AddUserReplyAsync(id, request, cancellationToken)));
        }
        catch (TicketClosedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/replies/agent")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> AddAgentReply(string id, AddAgentReplyRequest request, CancellationToken cancellationToken)
    {
        request.AgentId = User.AccountId();
        try
        {
            return Ok(await _ticketService.AddAgentReplyAsync(id, request, cancellationToken));
        }
        catch (TicketClosedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeStatusRequest(string Status, TicketActorRef ChangedBy);

    [HttpPut("{id}/status")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeStatus(string id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangeStatusAsync(id, request.Status, User.Actor(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangePriorityRequest(string Priority, TicketActorRef ChangedBy);

    [HttpPut("{id}/priority")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangePriority(string id, ChangePriorityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangePriorityAsync(id, request.Priority, User.Actor(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeTopicRequest(string Topic, TicketActorRef ChangedBy);

    [HttpPut("{id}/topic")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeTopic(string id, ChangeTopicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangeTopicAsync(id, request.Topic, User.Actor(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangePropertiesRequest(string Status, string Priority, string Topic, TicketActorRef ChangedBy);

    [HttpPut("{id}/properties")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> ChangeProperties(string id, ChangePropertiesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangePropertiesAsync(id, request.Status, request.Priority, request.Topic, User.Actor(), cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record AssignRequest(AssignTicketRequest Assignment, TicketActorRef AssignedBy);

    [HttpPut("{id}/assign")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> Assign(string id, AssignRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = request.Assignment?.Kind switch
            {
                "agent" => await _ticketService.AssignToAgentAsync(id, request.Assignment.Id, User.Actor(), cancellationToken),
                "team" => await _ticketService.AssignToTeamAsync(id, request.Assignment.Id, User.Actor(), cancellationToken),
                _ => throw new ArgumentException("Choose an agent or a team.")
            };
            return Ok(ticket);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>AgentId is ignored: the signed-in agent claims the ticket.</summary>
    public record ClaimRequest(string AgentId);

    [HttpPost("{id}/claim")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketDto>> Claim(string id, ClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ClaimAsync(id, User.AccountId(), cancellationToken));
        }
        catch (TicketConflictException ex)
        {
            // Another agent claimed/reassigned it between this agent loading the ticket and clicking Claim.
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record BulkStatusApiRequest(BulkStatusRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/status")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkStatus(BulkStatusApiRequest body, CancellationToken cancellationToken)
    {
        // TicketService validates the whole selection (<= 100 distinct, existing ids) before changing any.
        await _ticketService.BulkChangeStatusAsync(body.Request?.TicketIds!, body.Request?.Status ?? string.Empty, User.Actor(), cancellationToken);
        return Ok();
    }

    public record BulkPriorityApiRequest(BulkPriorityRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/priority")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkPriority(BulkPriorityApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkChangePriorityAsync(body.Request?.TicketIds!, body.Request?.Priority ?? string.Empty, User.Actor(), cancellationToken);
        return Ok();
    }

    public record BulkAssignApiRequest(BulkAssignRequest Request, TicketActorRef AssignedBy);

    [HttpPost("bulk/assign")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkAssign(BulkAssignApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkAssignAsync(body.Request?.TicketIds!, body.Request?.Kind ?? string.Empty, body.Request?.Id ?? string.Empty, User.Actor(), cancellationToken);
        return Ok();
    }

    public record BulkCloseApiRequest(BulkCloseRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/close")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<IActionResult> BulkClose(BulkCloseApiRequest body, CancellationToken cancellationToken)
    {
        var closed = await _ticketService.BulkCloseAsync(
            body.Request?.TicketIds!, body.Request?.Status ?? string.Empty, body.Request?.Note, User.Actor(), cancellationToken);
        return Ok(new { closed });
    }

    /// <summary>Unknown ticket -> 404; blank/over-long text, unknown/inactive assignee or an out-of-range due date -> 400.</summary>
    public record AddTaskRequest(string Text, string AssigneeAgentId, DateOnly? Due);

    [HttpPost("{id}/tasks")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketTaskDto>> AddTask(string id, AddTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _ticketService.AddTaskAsync(id, request.Text, request.AssigneeAgentId, request.Due, User.Actor(), cancellationToken);
        return Ok(task);
    }

    public record SetTaskDoneRequest(bool Done);

    /// <summary>Scoped to its ticket: a task id that isn't on ticket {id} (or an unknown ticket) is a 404.</summary>
    [HttpPut("{id}/tasks/{taskId}/done")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketTaskDto>> SetTaskDone(string id, string taskId, SetTaskDoneRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ticketService.SetTaskDoneAsync(id, taskId, request.Done, User.Actor(), cancellationToken));
    }

    [HttpGet("{id}/attachments")]
    public async Task<ActionResult<IReadOnlyList<TicketAttachmentDto>>> GetAttachments(string id, CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Ok(await _ticketService.GetAttachmentsAsync(id, cancellationToken));

        var ticket = await _ticketService.GetTicketAsync(id, cancellationToken);
        if (ticket is null || !User.IsOwnTicket(ticket)) return NotFound();
        return Ok(EndUserTicketAccess.VisibleAttachments(ticket, await _ticketService.GetAttachmentsAsync(id, cancellationToken)));
    }

    /// <summary>
    /// uploadedByType/uploadedById are ignored: the uploader is the signed-in account. End users
    /// only on their own tickets (404 otherwise). The rules (ticket open, allow-listed type, size
    /// and per-ticket caps, anchor entry) live in TicketService.AddAttachmentAsync; a rejected file
    /// is a 400 with { message }. The client's content type is ignored.
    /// </summary>
    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<TicketAttachmentDto>> UploadAttachment(
        string id, IFormFile? file, [FromForm] string? timelineEntryId, [FromForm] string? uploadedByType, [FromForm] string? uploadedById,
        CancellationToken cancellationToken)
    {
        if (User.IsEndUser() && (await _ticketService.GetTicketAsync(id, cancellationToken) is not { } ticket || !User.IsOwnTicket(ticket)))
        {
            return NotFound();
        }

        if (file is null) return BadRequest(new { message = "Choose a file to attach." });
        if (file.Length > TicketAttachmentRules.MaxFileSizeBytes)
        {
            return BadRequest(new { message = TicketAttachmentRules.TooLargeMessage });
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        try
        {
            var attachment = await _ticketService.AddAttachmentAsync(
                id, timelineEntryId, file.FileName, file.ContentType, stream.ToArray(),
                User.Actor(), cancellationToken);
            return Ok(attachment);
        }
        catch (Exception ex) when (ex is AttachmentTooLargeException or TicketClosedException or ArgumentException)
        {
            return BadRequest(new { message = ArgumentMessage(ex) });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>The exception message without the " (Parameter 'x')" suffix ArgumentException appends.</summary>
    private static string ArgumentMessage(Exception ex)
        => ex is ArgumentException { ParamName: { } param } ? ex.Message.Replace($" (Parameter '{param}')", string.Empty) : ex.Message;

    /// <summary>ActingAgentId ("mine" queue / "assigned to me" filter) is always the signed-in agent.</summary>
    [HttpGet("queue")]
    [Authorize(Policy = Policies.Agent)]
    public async Task<ActionResult<TicketQueuePageResult>> Queue([FromQuery] TicketQueueRequest request, CancellationToken cancellationToken)
    {
        request.ActingAgentId = User.AccountId();
        return Ok(await _queueService.SearchAsync(request, cancellationToken));
    }

    /// <summary>
    /// Sidebar badges for the signed-in account; the userId/agentId query values are ignored. End
    /// users get only their own MyOpenTickets count, agents the queue counts.
    /// </summary>
    [HttpGet("nav-counts")]
    public async Task<ActionResult<TicketNavCountsDto>> NavCounts(CancellationToken cancellationToken)
    {
        if (!User.IsEndUser()) return Ok(await _queueService.GetNavCountsAsync(null, User.AccountId(), cancellationToken));

        var counts = await _queueService.GetNavCountsAsync(User.AccountId(), null, cancellationToken);
        return Ok(new TicketNavCountsDto { MyOpenTickets = counts.MyOpenTickets });
    }
}
