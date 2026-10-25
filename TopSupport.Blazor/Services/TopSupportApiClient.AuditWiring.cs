using System.Net.Http.Json;
using TopSupport.Models.Tickets;

namespace TopSupport.Blazor.Services;

// The Change modal's single call, so the Api writes one "Changed ticket properties" audit entry.
public partial class TopSupportApiClient
{
    /// <summary>PUT api/tickets/{id}/properties -- ACT['change-go']: one call, one audit entry.</summary>
    public async Task<TicketDto> ChangeTicketPropertiesAsync(string ticketId, string status, string priority, string topic, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{Uri.EscapeDataString(ticketId)}/properties", new { status, priority, topic }, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(cancellationToken))!;
    }
}
