using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// CRUD passthrough over ICannedResponseRepository plus FillPlaceholders, porting
/// fillPlaceholders(text, t) from topsupport-app.js (~line 1546-1551).
/// </summary>
public interface ICannedResponseService
{
    Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CannedResponseDto> CreateAsync(CannedResponseDto response, CancellationToken cancellationToken = default);

    Task UpdateAsync(CannedResponseDto response, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Substitutes the 11 {ticket.*}/{recipient.*}/{agent.*} tokens in <paramref name="body"/>.
    /// When <paramref name="ticket"/> is null, uses the demo's exact fallback sample values
    /// (ticket #060012, requester "Amira Patel", etc.) so a preview still renders something
    /// sensible before a real ticket exists.
    /// </summary>
    Task<string> FillPlaceholdersAsync(string body, TicketDto? ticket, string? agentId, CancellationToken cancellationToken = default);
}
