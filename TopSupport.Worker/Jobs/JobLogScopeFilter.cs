using Hangfire.Server;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// Every log line written while a job runs carries its Hangfire JobId and job name (the job's
/// correlation id), so a run, its retries and the dashboard entry can be matched up.
/// </summary>
public sealed class JobLogScopeFilter : IServerFilter
{
    private const string ScopeKey = "TopSupport.LogScope";

    private readonly ILogger<JobLogScopeFilter> _logger;

    public JobLogScopeFilter(ILogger<JobLogScopeFilter> logger)
    {
        _logger = logger;
    }

    public void OnPerforming(PerformingContext context)
    {
        var scope = _logger.BeginScope("JobId:{JobId} Job:{Job}", context.BackgroundJob.Id, context.BackgroundJob.Job.Type.Name);
        context.Items[ScopeKey] = scope;
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Items.TryGetValue(ScopeKey, out var scope)) (scope as IDisposable)?.Dispose();
    }
}
