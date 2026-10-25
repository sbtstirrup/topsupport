namespace TopSupport.Models.Admin;

/// <summary>The admin Background Jobs page: TopSupport.Worker's servers, recurring jobs and recent runs.</summary>
public class WorkerJobsDto
{
    public List<WorkerServerDto> Servers { get; set; } = new();
    public List<RecurringJobDto> RecurringJobs { get; set; } = new();
    public List<JobRunDto> RecentRuns { get; set; } = new();
}

/// <summary>A running worker process, as Hangfire registers it (heartbeat about every 30 seconds).</summary>
public class WorkerServerDto
{
    public string Id { get; set; } = string.Empty;

    /// <summary>The machine part of Hangfire's server id ("topsupport.worker:host:pid:guid").</summary>
    public string Host { get; set; } = string.Empty;

    public DateTimeOffset LastHeartbeat { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
}

/// <summary>One of the worker's recurring jobs ("ticket-events", "sla-sweep", ...).</summary>
public class RecurringJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public DateTimeOffset? LastRun { get; set; }
    public DateTimeOffset? NextRun { get; set; }

    /// <summary>Hangfire state of the last run: Succeeded, Failed, Processing, Scheduled (waiting to retry) ...</summary>
    public string? LastState { get; set; }

    /// <summary>The last run's failure (or retry) message.</summary>
    public string? LastError { get; set; }

    /// <summary>Set by Hangfire when it couldn't schedule the job at all.</summary>
    public string? ScheduleError { get; set; }
}

/// <summary>One execution of a job.</summary>
public class JobRunDto
{
    public long JobId { get; set; }

    /// <summary>The recurring job it ran for; null for jobs started some other way.</summary>
    public string? RecurringJobId { get; set; }

    public string State { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StateAt { get; set; }
    public long? DurationMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>Emails the worker has sent (worker.NotificationLog), one page plus per-type counts.</summary>
public class WorkerNotificationsDto
{
    public List<NotificationCountDto> Counts { get; set; } = new();
    public int Total { get; set; }
    public List<NotificationLogItemDto> Items { get; set; } = new();
}

public class NotificationCountDto
{
    public string EventType { get; set; } = string.Empty;
    public int Last24Hours { get; set; }
    public int Last7Days { get; set; }
    public int AllTime { get; set; }
}

public class NotificationLogItemDto
{
    public long Id { get; set; }
    public DateTimeOffset SentAt { get; set; }

    /// <summary>Empty for emails that aren't about one ticket (the daily digest).</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Null when the ticket no longer exists.</summary>
    public string? TicketNumber { get; set; }
    public string? Subject { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string EventKey { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
}

/// <summary>The worker's email event types (the part of NotificationLog.EventKey before ':').</summary>
public static class WorkerEmailTypes
{
    public static readonly IReadOnlyList<(string Key, string Label)> All = new[]
    {
        ("created", "Ticket received"),
        ("reply", "Agent reply"),
        ("assigned", "Assigned to agent"),
        ("resolved", "Ticket resolved"),
        ("sla-breach", "Response target missed"),
        ("digest", "Daily digest")
    };

    public static string Label(string key) => All.FirstOrDefault(t => t.Key == key).Label ?? key;
}
