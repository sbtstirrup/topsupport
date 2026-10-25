using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;

namespace TopSupport.Blazor.Services;

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
public partial class TopSupportApiClient
{
    private readonly HttpClient _httpClient;

    // Each typed-client instance gets its own HttpClient, created in the signed-in user's circuit
    // (or request) scope, so the user's Api bearer token can go on its default headers.
    // Sign-in and sign-out are full page loads, which start a new circuit with the new user.
    public TopSupportApiClient(HttpClient httpClient, SessionState session)
    {
        _httpClient = httpClient;
        if (session.ApiToken is { } token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

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

    /// <summary>POST /api/tickets. Throws on failure -- callers should catch HttpRequestException
    /// (400 ValidationProblem) and surface field errors; the Api's error shape on a 400 is the
    /// standard ASP.NET ValidationProblemDetails (Errors: Dictionary&lt;string,string[]&gt;).</summary>
    public async Task<TicketDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    /// <summary>POST /api/tickets, returning the raw response so callers can inspect a 400
    /// ValidationProblem body (field -> messages) without relying on exception parsing.</summary>
    public Task<HttpResponseMessage> CreateTicketRawAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("api/tickets", request, cancellationToken);

    public async Task<TicketDto> AddUserReplyAsync(string ticketId, AddUserReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/replies/user", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> AddAgentReplyAsync(string ticketId, AddAgentReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/replies/agent", request, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangeStatusAsync(string ticketId, string status, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/status", new { status }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangePriorityAsync(string ticketId, string priority, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/priority", new { priority }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangeTopicAsync(string ticketId, string topic, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/topic", new { topic }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> AssignTicketAsync(string ticketId, AssignTicketRequest assignment, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/assign", assignment, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ClaimTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/tickets/{ticketId}/claim", null, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task BulkChangeStatusAsync(IEnumerable<string> ticketIds, string status, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/status",
            new { ticketIds = ticketIds.ToList(), status }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }

    public async Task BulkChangePriorityAsync(IEnumerable<string> ticketIds, string priority, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/priority",
            new { ticketIds = ticketIds.ToList(), priority }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }

    public async Task BulkAssignAsync(IEnumerable<string> ticketIds, string kind, string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/assign",
            new { ticketIds = ticketIds.ToList(), kind, id }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }

    /// <summary>
    /// POST api/tickets/bulk/close -- ACT['close-go']: the optional internal note and the close
    /// (Resolved or Closed) happen together server-side, after every ticket id has been validated,
    /// so a failing close never leaves notes behind. Already-Closed tickets are skipped.
    /// </summary>
    public async Task BulkCloseAsync(IEnumerable<string> ticketIds, string status, string? note, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets/bulk/close",
            new { ticketIds = ticketIds.ToList(), status, note }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }

    public async Task<TicketTaskDto> AddTaskAsync(string ticketId, string text, string assigneeAgentId, DateOnly? due, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/tasks", new { text, assigneeAgentId, due }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketTaskDto>(cancellationToken))!;
    }

    public async Task<TicketTaskDto> SetTaskDoneAsync(string ticketId, string taskId, bool done, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/tasks/{taskId}/done", new { done }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketTaskDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<TicketAttachmentDto>> GetAttachmentsAsync(string ticketId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TicketAttachmentDto>>($"api/tickets/{ticketId}/attachments", cancellationToken) ?? new();

    /// <summary>
    /// POST /api/tickets/{id}/attachments (multipart/form-data). 10MB/file cap is enforced
    /// both client-side (see FileUploadDropzone, OpenReadStream maxAllowedSize) and server-side
    /// (RequestSizeLimit and TicketAttachmentService -> 400). The uploader is the signed-in account.
    /// </summary>
    public async Task<TicketAttachmentDto> UploadAttachmentAsync(
        string ticketId, string fileName, string contentType, Stream content,
        string? timelineEntryId = null, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType);
        form.Add(streamContent, "file", fileName);
        if (timelineEntryId is not null) form.Add(new StringContent(timelineEntryId), "timelineEntryId");

        var response = await _httpClient.PostAsync($"api/tickets/{ticketId}/attachments", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // A rejected file is a 400 with { message } (or ProblemDetails' detail): surface that text.
            throw new HttpRequestException(await ReadUploadErrorAsync(response, cancellationToken), null, response.StatusCode);
        }

        return (await response.Content.ReadFromJsonAsync<TicketAttachmentDto>(cancellationToken))!;
    }

    /// <summary>Uploads a file staged in memory (see StagedAttachment) once the ticket/reply it belongs to exists.</summary>
    public Task<TicketAttachmentDto> UploadAttachmentAsync(
        string ticketId, TopSupport.Blazor.Models.StagedAttachment staged,
        string? timelineEntryId = null, CancellationToken cancellationToken = default)
        => UploadAttachmentAsync(ticketId, staged.Reference.Name, staged.Reference.ContentType, new MemoryStream(staged.Content, writable: false),
            timelineEntryId, cancellationToken);

    private static async Task<string> ReadUploadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        const string fallback = "The file could not be uploaded.";
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return "That ticket could not be found.";
        if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge) return TicketAttachmentRules.TooLargeMessage;

        try
        {
            using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            foreach (var name in new[] { "message", "detail" })
            {
                if (body.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                    body.RootElement.TryGetProperty(name, out var value) &&
                    value.ValueKind == System.Text.Json.JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString()!;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return fallback;
    }

    /// <summary>Convenience overload that reads straight from an IBrowserFile (10MB cap applied by the caller).</summary>
    public Task<TicketAttachmentDto> UploadAttachmentAsync(
        string ticketId, IBrowserFile file,
        long maxAllowedSize = 10 * 1024 * 1024, string? timelineEntryId = null, CancellationToken cancellationToken = default)
        => UploadAttachmentAsync(ticketId, file.Name, file.ContentType, file.OpenReadStream(maxAllowedSize, cancellationToken),
            timelineEntryId, cancellationToken);

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

    /// <summary>The signed-in account's sidebar badges.</summary>
    public async Task<TicketNavCountsDto> GetNavCountsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<TicketNavCountsDto>("api/tickets/nav-counts", cancellationToken) ?? new();

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

    /// <summary>POST /api/CannedResponses/{id}/preview -- fills the body's placeholder tokens against a real ticket and the signed-in agent.</summary>
    public async Task<string> PreviewCannedResponseAsync(string id, string? ticketId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync($"api/CannedResponses/{id}/preview", new { ticketId }, cancellationToken);
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
