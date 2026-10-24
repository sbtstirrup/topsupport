using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Queue membership + filter + sort + paginate, wrapping ITicketRepository.
/// GetActiveForQueueAsync/SearchForQueueAsync. Implements the 5 built-in queues from the
/// demo's QUEUES map (open/mine/overdue/answered/closed) plus free filtering/sorting/paging
/// (BLANK_FILTERS/applyFilters/thSort/pager in topsupport-app.js), all in C#/LINQ over the
/// returned rows — not pushed into SQL.
/// </summary>
public interface IQueueService
{
    Task<TicketQueuePageResult> SearchAsync(TicketQueueRequest request, CancellationToken cancellationToken = default);

    /// <summary>Backs GET /api/tickets/nav-counts.</summary>
    Task<TicketNavCountsDto> GetNavCountsAsync(string? userId, string? agentId, CancellationToken cancellationToken = default);
}
