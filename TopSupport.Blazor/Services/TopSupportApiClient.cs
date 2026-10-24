using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;

namespace TopSupport.Blazor.Services;

public record HealthStatus(string Status, DateTimeOffset Utc);

/// <summary>
/// Typed HttpClient for calling TopSupport.Api, registered via AddHttpClient in Program.cs
/// with its BaseAddress bound to the "TopSupportApi:BaseUrl" configuration value. This is
/// the real HTTP boundary between the Blazor Server app and the middle-tier API -- even
/// though both happen to run server-side, calls go over the network, not in-process.
///
/// Every method here corresponds 1:1 to a TopSupport.Api route -- see TicketsController,
/// AttachmentsController, CannedResponsesController and LookupController for the ground-truth
/// route strings / request-response shapes this client must stay in sync with.
/// </summary>
public class TopSupportApiClient
{
    private readonly HttpClient _httpClient;

    public TopSupportApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Calls GET /api/health -- the one real end-to-end call wired up in the skeleton pass.</summary>
    public async Task<HealthStatus?> GetHealthAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<HealthStatus>("api/health", cancellationToken);

    // ---------------- Tickets ----------------

    public async Task<TicketDto?> GetTicketAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/tickets/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken);
    }

    /// <summary>"My Tickets": every ticket where the acting user is requester or affected.</summary>
    public async Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(string requesterUserId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TicketDto>>($"api/tickets/by-requester/{requesterUserId}", cancellationToken) ?? new();

    public async Task<IReadOnlyList<TicketTopic>> GetTopicsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TicketTopic>>("api/tickets/topics", cancellationToken) ?? new();

    /// <summary>Resolved field descriptors (dynamic options filled in) for one topic's form.</summary>
    public async Task<IReadOnlyList<TicketFieldDescriptor>> GetFieldSchemaAsync(string topic, string? affectedUserId, CancellationToken cancellationToken = default)
    {
        var url = $"api/tickets/field-schema/{Uri.EscapeDataString(topic)}";
        if (!string.IsNullOrEmpty(affectedUserId))
        {
            url += $"?affectedUserId={Uri.EscapeDataString(affectedUserId)}";
        }

        return await _httpClient.GetFromJsonAsync<List<TicketFieldDescriptor>>(url, cancellationToken) ?? new();
    }

    public record CreateTicketApiRequest(CreateTicketRequest Request, TicketActorRef CreatedBy);

    /// <summary>POST /api/tickets. Throws on failure -- callers should catch HttpRequestException
    /// (400 ValidationProblem) and surface field errors; the Api's error shape on a 400 is the
    /// standard ASP.NET ValidationProblemDetails (Errors: Dictionary&lt;string,string[]&gt;).</summary>
    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, TicketActorRef createdBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets", new CreateTicketApiRequest(request, createdBy), cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    /// <summary>POST /api/tickets, returning the raw response so callers can inspect a 400
    /// ValidationProblem body (field -> messages) without relying on exception parsing.</summary>
    public Task<HttpResponseMessage> CreateTicketRawAsync(CreateTicketRequest request, TicketActorRef createdBy, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("api/tickets", new CreateTicketApiRequest(request, createdBy), cancellationToken);

    public async Task<TicketDto> AddUserReplyAsync(string ticketId, AddUserReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/replies/user", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> AddAgentReplyAsync(string ticketId, AddAgentReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/replies/agent", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangeStatusAsync(string ticketId, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/status", new { status, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangePriorityAsync(string ticketId, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/priority", new { priority, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangeTopicAsync(string ticketId, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/topic", new { topic, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> AssignTicketAsync(string ticketId, AssignTicketRequest assignment, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/assign", new { assignment, assignedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ClaimTicketAsync(string ticketId, string agentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/claim", new { agentId }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task BulkChangeStatusAsync(IEnumerable<string> ticketIds, string status, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/status",
            new { request = new { ticketIds = ticketIds.ToList(), status }, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkChangePriorityAsync(IEnumerable<string> ticketIds, string priority, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/priority",
            new { request = new { ticketIds = ticketIds.ToList(), priority }, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkAssignAsync(IEnumerable<string> ticketIds, string kind, string id, TicketActorRef assignedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/assign",
            new { request = new { ticketIds = ticketIds.ToList(), kind, id }, assignedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task BulkCloseAsync(IEnumerable<string> ticketIds, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/close",
            new { request = new { ticketIds = ticketIds.ToList() }, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TicketTaskDto> AddTaskAsync(string ticketId, string text, string assigneeAgentId, DateOnly? due, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/tasks", new { text, assigneeAgentId, due }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketTaskDto>(cancellationToken))!;
    }

    public async Task SetTaskDoneAsync(string taskId, bool done, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/tasks/{taskId}/done", new { done }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<TicketAttachmentDto>> GetAttachmentsAsync(string ticketId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TicketAttachmentDto>>($"api/tickets/{ticketId}/attachments", cancellationToken) ?? new();

    /// <summary>
    /// POST /api/tickets/{id}/attachments (multipart/form-data). 10MB/file cap is enforced
    /// both client-side (see FileUploadDropzone, OpenReadStream maxAllowedSize) and server-side
    /// (RequestSizeLimit + AttachmentTooLargeException -> 400).
    /// </summary>
    public async Task<TicketAttachmentDto> UploadAttachmentAsync(
        string ticketId, string fileName, string contentType, Stream content,
        string uploadedByType, string? uploadedById, string? timelineEntryId = null,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
        form.Add(streamContent, "file", fileName);
        form.Add(new StringContent(uploadedByType), "uploadedByType");
        if (uploadedById is not null) form.Add(new StringContent(uploadedById), "uploadedById");
        if (timelineEntryId is not null) form.Add(new StringContent(timelineEntryId), "timelineEntryId");

        var response = await _httpClient.PostAsync($"api/tickets/{ticketId}/attachments", form, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketAttachmentDto>(cancellationToken))!;
    }

    /// <summary>Convenience overload that reads straight from an IBrowserFile (10MB cap applied by the caller).</summary>
    public Task<TicketAttachmentDto> UploadAttachmentAsync(
        string ticketId, IBrowserFile file, string uploadedByType, string? uploadedById,
        long maxAllowedSize = 10 * 1024 * 1024, string? timelineEntryId = null, CancellationToken cancellationToken = default)
        => UploadAttachmentAsync(ticketId, file.Name, file.ContentType, file.OpenReadStream(maxAllowedSize, cancellationToken),
            uploadedByType, uploadedById, timelineEntryId, cancellationToken);

    public async Task<TicketQueuePageResult> GetQueueAsync(TicketQueueRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildQueueQueryString(request);
        return await _httpClient.GetFromJsonAsync<TicketQueuePageResult>($"api/tickets/queue{query}", cancellationToken) ?? new();
    }

    private static string BuildQueueQueryString(TicketQueueRequest r)
    {
        var parts = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value)) parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        Add(nameof(r.Queue), r.Queue);
        Add(nameof(r.ActingAgentId), r.ActingAgentId);
        Add(nameof(r.SearchText), r.SearchText);
        Add(nameof(r.Status), r.Status);
        Add(nameof(r.Priority), r.Priority);
        Add(nameof(r.Topic), r.Topic);
        Add(nameof(r.Department), r.Department);
        Add(nameof(r.Assignee), r.Assignee);
        Add(nameof(r.SortKey), r.SortKey);
        parts.Add($"{nameof(r.SortDescending)}={r.SortDescending}");
        parts.Add($"{nameof(r.Page)}={r.Page}");
        parts.Add($"{nameof(r.PageSize)}={r.PageSize}");

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public async Task<TicketNavCountsDto> GetNavCountsAsync(string? userId, string? agentId, CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(userId)) parts.Add($"userId={Uri.EscapeDataString(userId)}");
        if (!string.IsNullOrEmpty(agentId)) parts.Add($"agentId={Uri.EscapeDataString(agentId)}");
        var query = parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);

        return await _httpClient.GetFromJsonAsync<TicketNavCountsDto>($"api/tickets/nav-counts{query}", cancellationToken) ?? new();
    }

    // ---------------- Attachments (content download) ----------------

    /// <summary>GET /api/attachments/{id}/content -- raw file bytes. Returns null on 404.</summary>
    public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/attachments/{attachmentId}/content", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? attachmentId;
        return (bytes, contentType, fileName.Trim('"'));
    }

    // ---------------- Canned responses ----------------

    public async Task<IReadOnlyList<CannedResponseDto>> GetCannedResponsesAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<CannedResponseDto>>("api/CannedResponses", cancellationToken) ?? new();

    public async Task<CannedResponseDto> CreateCannedResponseAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync("api/CannedResponses", response, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        return (await httpResponse.Content.ReadFromJsonAsync<CannedResponseDto>(cancellationToken))!;
    }

    public async Task UpdateCannedResponseAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PutAsJsonAsync($"api/CannedResponses/{response.Id}", response, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
    }

    public async Task DeleteCannedResponseAsync(string id, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.DeleteAsync($"api/CannedResponses/{id}", cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
    }

    /// <summary>POST /api/CannedResponses/{id}/preview -- fills the body's placeholder tokens against a real ticket/agent.</summary>
    public async Task<string> PreviewCannedResponseAsync(string id, string? ticketId, string? agentId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync($"api/CannedResponses/{id}/preview", new { ticketId, agentId }, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        var body = await httpResponse.Content.ReadFromJsonAsync<PreviewResult>(cancellationToken);
        return body?.Text ?? string.Empty;
    }

    private record PreviewResult(string Text);

    // ---------------- Lookup data (users/agents/teams/assets) ----------------

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<UserDto>>("api/lookup/users", cancellationToken) ?? new();

    public async Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<AgentDto>>("api/lookup/agents", cancellationToken) ?? new();

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TeamDto>>("api/lookup/teams", cancellationToken) ?? new();

    /// <summary>Assets belonging to one user. There is no "all assets" lookup (see LookupController) --
    /// userId is required.</summary>
    public async Task<IReadOnlyList<AssetDto>> GetAssetsByUserAsync(string userId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<AssetDto>>($"api/lookup/assets?userId={Uri.EscapeDataString(userId)}", cancellationToken) ?? new();
}
