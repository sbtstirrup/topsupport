using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// The admin Background Jobs page: TopSupport.Worker's recurring jobs and the emails it has sent.
/// Read-only; the worker itself is managed from its Hangfire dashboard on the worker's host.
/// </summary>
public interface IWorkerMonitorService
{
    Task<WorkerJobsDto> GetJobsAsync(CancellationToken cancellationToken = default);

    Task<WorkerNotificationsDto> GetNotificationsAsync(string? eventType, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}

public class WorkerMonitorService : IWorkerMonitorService
{
    public const int RecentRunCount = 50;
    public const int MaxPageSize = 100;

    private readonly IWorkerMonitorRepository _repository;

    public WorkerMonitorService(IWorkerMonitorRepository repository)
    {
        _repository = repository;
    }

    public Task<WorkerJobsDto> GetJobsAsync(CancellationToken cancellationToken = default)
        => _repository.GetJobsAsync(RecentRunCount, cancellationToken);

    public Task<WorkerNotificationsDto> GetNotificationsAsync(string? eventType, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Unknown types are rejected rather than silently showing nothing.
        if (!string.IsNullOrEmpty(eventType) && WorkerEmailTypes.All.All(t => t.Key != eventType))
        {
            throw new ArgumentException($"Unknown email type '{eventType}'.", nameof(eventType));
        }

        var term = search?.Trim();
        if (term is { Length: > FieldLimits.SearchText }) term = term[..FieldLimits.SearchText];

        return _repository.GetNotificationsAsync(
            string.IsNullOrEmpty(eventType) ? null : eventType,
            string.IsNullOrEmpty(term) ? null : EscapeLike(term),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, MaxPageSize),
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    /// <summary>"%", "_" and "[" match literally in the proc's LIKE.</summary>
    private static string EscapeLike(string s) => s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
