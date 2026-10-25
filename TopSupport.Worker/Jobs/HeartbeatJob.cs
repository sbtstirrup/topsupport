using Hangfire;
using TopSupport.Worker.Metrics;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// "heartbeat" (every 5 minutes): a log line, and a CloudWatch "Heartbeat" metric when
/// "CloudWatch:Enabled" is on (it's off locally), so an alarm can spot a stopped worker.
/// </summary>
public sealed class HeartbeatJob
{
    public const string JobName = "heartbeat";

    private readonly IMetricsPublisher _metrics;
    private readonly TimeProvider _time;
    private readonly ILogger<HeartbeatJob> _logger;

    public HeartbeatJob(IMetricsPublisher metrics, TimeProvider time, ILogger<HeartbeatJob> logger)
    {
        _metrics = metrics;
        _time = time;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        _logger.LogInformation("heartbeat: TopSupport.Worker on {Host} is running at {Now:u}.", Environment.MachineName, now);
        await _metrics.PublishHeartbeatAsync(now, cancellationToken);
    }
}
