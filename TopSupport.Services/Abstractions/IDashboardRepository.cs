namespace TopSupport.Services.Abstractions;

/// <summary>Data-access contract for the admin dashboard, calling usp_Dashboard_GetTicketClosedAt
/// (TopSupport.Database/Scripts/12_Admin_Dashboard.sql).</summary>
public interface IDashboardRepository
{
    /// <summary>closedAt(t) for every Resolved/Closed ticket, keyed by ticket id.</summary>
    Task<IReadOnlyDictionary<string, DateTimeOffset>> GetTicketClosedAtAsync(CancellationToken cancellationToken = default);
}
