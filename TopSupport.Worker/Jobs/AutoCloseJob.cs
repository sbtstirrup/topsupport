using Hangfire;
using Microsoft.Extensions.Options;
using TopSupport.Worker.Data;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// "auto-close" (daily, 02:00 Europe/London): tickets Resolved for 7+ days become Closed, with the
/// same "Closed by System with status Closed" timeline event and "Changed status to Closed" audit
/// entry the Api writes (worker.usp_AutoClose_CloseTicket). The worker's only write to dbo tables.
/// </summary>
public sealed class AutoCloseJob
{
    public const string JobName = "auto-close";

    private readonly IWorkerTicketStore _store;
    private readonly TimeProvider _time;
    private readonly JobOptions _jobs;
    private readonly ILogger<AutoCloseJob> _logger;

    public AutoCloseJob(IWorkerTicketStore store, TimeProvider time, IOptions<JobOptions> jobs, ILogger<AutoCloseJob> logger)
    {
        _store = store;
        _time = time;
        _jobs = jobs.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var cutoff = now.AddDays(-_jobs.AutoCloseAfterDays);

        var closed = 0;
        foreach (var t in await _store.GetResolvedTicketsAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (t.ResolvedAt > cutoff) continue;

            if (await _store.CloseResolvedTicketAsync(t.TicketId, now, cancellationToken))
            {
                closed++;
                _logger.LogInformation("auto-close: closed #{Number}, resolved {ResolvedAt:u}.", t.Number, t.ResolvedAt);
            }
        }

        _logger.LogInformation("auto-close: {Closed} ticket(s) closed (resolved on or before {Cutoff:u}).", closed, cutoff);
    }
}
