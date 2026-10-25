using System.Net.Http.Json;
using TopSupport.Models.Tickets;

namespace TopSupport.Blazor.Services;

// Variants of existing ticket/canned-response calls that carry what the Api needs to write the
// demo's audit entries: a single "Changed ticket properties" entry for the Change modal, and the
// acting agent's id for canned-response create/edit/delete.
public partial class TopSupportApiClient
{
    /// <summary>PUT api/tickets/{id}/properties -- ACT['change-go']: one call, one audit entry.</summary>
    public async Task<TicketDto> ChangeTicketPropertiesAsync(string ticketId, string status, string priority, string topic, TicketActorRef changedBy, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/properties", new { status, priority, topic, changedBy }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }

    public async Task<CannedResponseDto> CreateCannedResponseAsync(CannedResponseDto response, string actorId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync($"api/CannedResponses?actorId={Uri.EscapeDataString(actorId)}", response, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        return (await httpResponse.Content.ReadFromJsonAsync<CannedResponseDto>(cancellationToken))!;
    }

    public async Task UpdateCannedResponseAsync(CannedResponseDto response, string actorId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.PutAsJsonAsync($"api/CannedResponses/{response.Id}?actorId={Uri.EscapeDataString(actorId)}", response, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
    }

    public async Task DeleteCannedResponseAsync(string id, string actorId, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _httpClient.DeleteAsync($"api/CannedResponses/{id}?actorId={Uri.EscapeDataString(actorId)}", cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
    }
}
