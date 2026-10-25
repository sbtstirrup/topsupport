using System.Net.Http.Json;
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
        var response = await _httpClient.GetAsync($"api/tickets/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken);
    }

    /// <summary>"My Tickets": every ticket where the acting user is requester or affected.</summary>
    public async Task<IReadOnlyList<TicketDto>> GetMyTicketsAsync(string requesterUserId, CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<TicketDto>>($"api/tickets/by-requester/{Uri.EscapeDataString(requesterUserId)}", cancellationToken);

    public async Task<IReadOnlyList<TicketTopic>> GetTopicsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<TicketTopic>>("api/tickets/topics", cancellationToken);

    /// <summary>Resolved field descriptors (dynamic options filled in) for one topic's form.</summary>
    public async Task<IReadOnlyList<TicketFieldDescriptor>> GetFieldSchemaAsync(string topic, string? affectedUserId, CancellationToken cancellationToken = default)
    {
        var url = $"api/tickets/field-schema/{Uri.EscapeDataString(topic)}";
        if (!string.IsNullOrEmpty(affectedUserId))
        {
            url += $"?affectedUserId={Uri.EscapeDataString(affectedUserId)}";
        }

        return await GetRequiredAsync<List<TicketFieldDescriptor>>(url, cancellationToken);
    }

    /// <summary>POST /api/tickets, returning the raw response so callers can inspect a 400
    /// ValidationProblem body (field -> messages) without relying on exception parsing.</summary>
    public Task<HttpResponseMessage> CreateTicketRawAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("api/tickets", request, cancellationToken);

    public async Task<TicketDto> AddUserReplyAsync(string ticketId, AddUserReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/replies/user", request, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> AddAgentReplyAsync(string ticketId, AddAgentReplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/replies/agent", request, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ChangeStatusAsync(string ticketId, string status, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/status", new { status }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<TicketDto> ClaimTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/claim", null, cancellationToken);
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

        var response = await _httpClient.PostAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/attachments", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // A rejected file is a 400 with { message } (or ProblemDetails' detail): surface that text.
            throw new ApiProblemException(await ReadUploadErrorAsync(response, cancellationToken), response.StatusCode);
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

    public async Task<TicketQueuePageResult> GetQueueAsync(TicketQueueRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildQueueQueryString(request);
        return await GetRequiredAsync<TicketQueuePageResult>($"api/tickets/queue{query}", cancellationToken);
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
        => await GetRequiredAsync<TicketNavCountsDto>("api/tickets/nav-counts", cancellationToken);

    // ---------------- Attachments (content download) ----------------

    /// <summary>GET /api/attachments/{id}/content -- raw file bytes. Returns null on 404.</summary>
    public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/attachments/{Uri.EscapeDataString(attachmentId)}/content", cancellationToken);
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
        => await GetRequiredAsync<List<CannedResponseDto>>("api/CannedResponses", cancellationToken);

    /// <summary>The created response, or the Api's field errors ("title", "body") from a 400.</summary>
    public async Task<UserOrgSaveResult<CannedResponseDto>> CreateCannedResponseAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync("api/CannedResponses", response, cancellationToken);
        return await ReadSaveResultAsync<CannedResponseDto>(httpResponse, cancellationToken);
    }

    /// <summary>No value (the Api returns no body), or the Api's field errors ("title", "body") from a 400.</summary>
    public async Task<UserOrgSaveResult<CannedResponseDto>> UpdateCannedResponseAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PutAsJsonAsync($"api/CannedResponses/{Uri.EscapeDataString(response.Id)}", response, cancellationToken);
        if (httpResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new UserOrgSaveResult<CannedResponseDto>(null, await ReadValidationErrorsAsync(httpResponse, cancellationToken));
        }

        await EnsureSuccessWithMessageAsync(httpResponse, cancellationToken);
        return new UserOrgSaveResult<CannedResponseDto>(null, new Dictionary<string, string>());
    }

    public async Task DeleteCannedResponseAsync(string id, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.DeleteAsync($"api/CannedResponses/{Uri.EscapeDataString(id)}", cancellationToken);
        await EnsureSuccessWithMessageAsync(httpResponse, cancellationToken);
    }

    /// <summary>POST /api/CannedResponses/{id}/preview -- fills the body's placeholder tokens against a real ticket and the signed-in agent.</summary>
    public async Task<string> PreviewCannedResponseAsync(string id, string? ticketId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync($"api/CannedResponses/{Uri.EscapeDataString(id)}/preview", new { ticketId }, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        var body = await httpResponse.Content.ReadFromJsonAsync<PreviewResult>(cancellationToken);
        return body?.Text ?? string.Empty;
    }

    private record PreviewResult(string Text);

    // ---------------- Lookup data (users/agents/teams/assets) ----------------

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<UserDto>>("api/lookup/users", cancellationToken);

    public async Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<AgentDto>>("api/lookup/agents", cancellationToken);

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<TeamDto>>("api/lookup/teams", cancellationToken);

    /// <summary>Assets belonging to one user. There is no "all assets" lookup (see LookupController) --
    /// userId is required.</summary>
    public async Task<IReadOnlyList<AssetDto>> GetAssetsByUserAsync(string userId, CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<AssetDto>>($"api/lookup/assets?userId={Uri.EscapeDataString(userId)}", cancellationToken);
}
