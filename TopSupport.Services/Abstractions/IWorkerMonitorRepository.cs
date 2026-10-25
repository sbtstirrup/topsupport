using TopSupport.Models.Admin;

namespace TopSupport.Services.Abstractions;

/// <summary>Read-only access to TopSupport.Worker's state (17_Admin_WorkerMonitor.sql).</summary>
public interface IWorkerMonitorRepository
{
    Task<WorkerJobsDto> GetJobsAsync(int recentRuns, CancellationToken cancellationToken = default);

    /// <summary><paramref name="search"/> is already LIKE-escaped.</summary>
    Task<WorkerNotificationsDto> GetNotificationsAsync(string? eventType, string? search, int page, int pageSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
