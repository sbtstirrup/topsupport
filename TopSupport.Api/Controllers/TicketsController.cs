using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Ticket endpoints: CRUD/reply/status/priority/assign/bulk actions, topic catalog +
/// field-schema, queue search, task add/set-done, attachment list, nav-counts. Attachment
/// content download lives on AttachmentsController; canned-response CRUD/preview lives on
/// CannedResponsesController. No merge endpoint — ticket merge is out of scope.
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
        return ticket is null ? NotFound() : Ok(ticket);
    }

    /// <summary>"My Tickets": every ticket where the user is requester or affected.</summary>
    [HttpGet("by-requester/{requesterUserId}")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetByRequester(string requesterUserId, CancellationToken cancellationToken)
    {
        var tickets = await _ticketService.GetMyTicketsAsync(requesterUserId, cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("topics")]
    public ActionResult<IReadOnlyList<TicketTopic>> GetTopics() => Ok(TicketTopics.TOPICS);

    [HttpGet("field-schema/{topic}")]
    public async Task<ActionResult<List<TicketFieldDescriptor>>> GetFieldSchema(
        string topic, [FromQuery] string? affectedUserId, CancellationToken cancellationToken)
    {
        if (TicketTopics.Find(topic) is null) return NotFound();
        var fields = await _optionsResolver.ResolveAsync(topic, affectedUserId, new Dictionary<string, object?>(), cancellationToken);
        return Ok(fields);
    }

    public record CreateTicketApiRequest(CreateTicketRequest Request, TicketActorRef CreatedBy);

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketApiRequest body, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.CreateTicketAsync(body.Request, body.CreatedBy, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
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

    [HttpPost("{id}/replies/user")]
    public async Task<ActionResult<TicketDto>> AddUserReply(string id, AddUserReplyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.AddUserReplyAsync(id, request, cancellationToken));
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
    public async Task<ActionResult<TicketDto>> AddAgentReply(string id, AddAgentReplyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.AddAgentReplyAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeStatusRequest(string Status, TicketActorRef ChangedBy);

    [HttpPut("{id}/status")]
    public async Task<ActionResult<TicketDto>> ChangeStatus(string id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangeStatusAsync(id, request.Status, request.ChangedBy, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangePriorityRequest(string Priority, TicketActorRef ChangedBy);

    [HttpPut("{id}/priority")]
    public async Task<ActionResult<TicketDto>> ChangePriority(string id, ChangePriorityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangePriorityAsync(id, request.Priority, request.ChangedBy, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeTopicRequest(string Topic, TicketActorRef ChangedBy);

    [HttpPut("{id}/topic")]
    public async Task<ActionResult<TicketDto>> ChangeTopic(string id, ChangeTopicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ChangeTopicAsync(id, request.Topic, request.ChangedBy, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record AssignRequest(AssignTicketRequest Assignment, TicketActorRef AssignedBy);

    [HttpPut("{id}/assign")]
    public async Task<ActionResult<TicketDto>> Assign(string id, AssignRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = request.Assignment.Kind == "agent"
                ? await _ticketService.AssignToAgentAsync(id, request.Assignment.Id, request.AssignedBy, cancellationToken)
                : await _ticketService.AssignToTeamAsync(id, request.Assignment.Id, request.AssignedBy, cancellationToken);
            return Ok(ticket);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ClaimRequest(string AgentId);

    [HttpPost("{id}/claim")]
    public async Task<ActionResult<TicketDto>> Claim(string id, ClaimRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _ticketService.ClaimAsync(id, request.AgentId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record BulkStatusApiRequest(BulkStatusRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/status")]
    public async Task<IActionResult> BulkStatus(BulkStatusApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkChangeStatusAsync(body.Request.TicketIds, body.Request.Status, body.ChangedBy, cancellationToken);
        return Ok();
    }

    public record BulkPriorityApiRequest(BulkPriorityRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/priority")]
    public async Task<IActionResult> BulkPriority(BulkPriorityApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkChangePriorityAsync(body.Request.TicketIds, body.Request.Priority, body.ChangedBy, cancellationToken);
        return Ok();
    }

    public record BulkAssignApiRequest(BulkAssignRequest Request, TicketActorRef AssignedBy);

    [HttpPost("bulk/assign")]
    public async Task<IActionResult> BulkAssign(BulkAssignApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkAssignAsync(body.Request.TicketIds, body.Request.Kind, body.Request.Id, body.AssignedBy, cancellationToken);
        return Ok();
    }

    public record BulkCloseApiRequest(BulkCloseRequest Request, TicketActorRef ChangedBy);

    [HttpPost("bulk/close")]
    public async Task<IActionResult> BulkClose(BulkCloseApiRequest body, CancellationToken cancellationToken)
    {
        await _ticketService.BulkCloseAsync(body.Request.TicketIds, body.ChangedBy, cancellationToken);
        return Ok();
    }

    public record AddTaskRequest(string Text, string AssigneeAgentId, DateOnly? Due);

    [HttpPost("{id}/tasks")]
    public async Task<ActionResult<TicketTaskDto>> AddTask(string id, AddTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _ticketService.AddTaskAsync(id, request.Text, request.AssigneeAgentId, request.Due, cancellationToken);
        return Ok(task);
    }

    public record SetTaskDoneRequest(bool Done);

    [HttpPut("tasks/{taskId}/done")]
    public async Task<IActionResult> SetTaskDone(string taskId, SetTaskDoneRequest request, CancellationToken cancellationToken)
    {
        await _ticketService.SetTaskDoneAsync(taskId, request.Done, cancellationToken);
        return Ok();
    }

    [HttpGet("{id}/attachments")]
    public async Task<ActionResult<IReadOnlyList<TicketAttachmentDto>>> GetAttachments(string id, CancellationToken cancellationToken)
        => Ok(await _ticketService.GetAttachmentsAsync(id, cancellationToken));

    [HttpPost("{id}/attachments")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<TicketAttachmentDto>> UploadAttachment(
        string id, IFormFile file, [FromForm] string? timelineEntryId, [FromForm] string uploadedByType, [FromForm] string? uploadedById,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        try
        {
            var attachment = await _ticketService.AddAttachmentAsync(
                id, timelineEntryId, file.FileName, file.ContentType, stream.ToArray(),
                new TicketActorRef { Type = uploadedByType, Id = uploadedById }, cancellationToken);
            return Ok(attachment);
        }
        catch (AttachmentTooLargeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("queue")]
    public async Task<ActionResult<TicketQueuePageResult>> Queue([FromQuery] TicketQueueRequest request, CancellationToken cancellationToken)
        => Ok(await _queueService.SearchAsync(request, cancellationToken));

    [HttpGet("nav-counts")]
    public async Task<ActionResult<TicketNavCountsDto>> NavCounts(
        [FromQuery] string? userId, [FromQuery] string? agentId, CancellationToken cancellationToken)
        => Ok(await _queueService.GetNavCountsAsync(userId, agentId, cancellationToken));
}
