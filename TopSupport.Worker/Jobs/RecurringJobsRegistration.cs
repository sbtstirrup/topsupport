using Hangfire;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// Registers the five recurring jobs at start-up (AddOrUpdate, so changed schedules in config
/// replace the stored ones). Schedules are cron in Europe/London; Hangfire swaps the
/// CancellationToken.None placeholder for the job's real cancellation token.
/// </summary>
public static class RecurringJobsRegistration
{
    public static void Register(IRecurringJobManager jobs, JobOptions options)
    {
        var timeZone = new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone) };

        jobs.AddOrUpdate<TicketEventsJob>(TicketEventsJob.JobName, j => j.RunAsync(CancellationToken.None), options.TicketEventsCron, timeZone);
        jobs.AddOrUpdate<SlaSweepJob>(SlaSweepJob.JobName, j => j.RunAsync(CancellationToken.None), options.SlaSweepCron, timeZone);
        jobs.AddOrUpdate<AutoCloseJob>(AutoCloseJob.JobName, j => j.RunAsync(CancellationToken.None), options.AutoCloseCron, timeZone);
        jobs.AddOrUpdate<DailyDigestJob>(DailyDigestJob.JobName, j => j.RunAsync(CancellationToken.None), options.DailyDigestCron, timeZone);
        jobs.AddOrUpdate<HeartbeatJob>(HeartbeatJob.JobName, j => j.RunAsync(CancellationToken.None), options.HeartbeatCron, timeZone);
    }
}
