using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Options;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Metrics;

/// <summary>Publishes the worker's heartbeat metric.</summary>
public interface IMetricsPublisher
{
    Task PublishHeartbeatAsync(DateTimeOffset atUtc, CancellationToken cancellationToken);
}

/// <summary>Used when "CloudWatch:Enabled" is false (the default, and always locally).</summary>
public sealed class NullMetricsPublisher : IMetricsPublisher
{
    public Task PublishHeartbeatAsync(DateTimeOffset atUtc, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// A "Heartbeat" custom metric (value 1, dimension Host) in the configured namespace, so a
/// CloudWatch alarm on missing data can flag a stopped worker. Credentials come from the
/// instance role via the SDK's default chain.
/// </summary>
public sealed class CloudWatchMetricsPublisher : IMetricsPublisher
{
    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly CloudWatchOptions _options;

    public CloudWatchMetricsPublisher(IAmazonCloudWatch cloudWatch, IOptions<CloudWatchOptions> options)
    {
        _cloudWatch = cloudWatch;
        _options = options.Value;
    }

    public Task PublishHeartbeatAsync(DateTimeOffset atUtc, CancellationToken cancellationToken)
        => _cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
        {
            Namespace = _options.Namespace,
            MetricData = new List<MetricDatum>
            {
                new()
                {
                    MetricName = "Heartbeat",
                    Value = 1,
                    Unit = StandardUnit.Count,
                    Timestamp = atUtc.UtcDateTime,
                    Dimensions = new List<Dimension> { new() { Name = "Host", Value = Environment.MachineName } }
                }
            }
        }, cancellationToken);
}
