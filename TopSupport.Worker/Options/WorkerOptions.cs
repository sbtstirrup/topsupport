namespace TopSupport.Worker.Options;

/// <summary>"App": where the TopSupport Blazor app lives, for the ticket links in every email.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    public string BaseUrl { get; set; } = "https://localhost:7119";

    /// <summary>The end-user ticket page (TicketDetail.razor).</summary>
    public string UserTicketUrl(string ticketId) => $"{BaseUrl.TrimEnd('/')}/tickets/{Uri.EscapeDataString(ticketId)}";

    /// <summary>The agent ticket page (AgentTicketDetail.razor).</summary>
    public string AgentTicketUrl(string ticketId) => $"{BaseUrl.TrimEnd('/')}/agent-tickets/{Uri.EscapeDataString(ticketId)}";
}

/// <summary>"Email": which IEmailSender is used and who emails come from.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Smtp" (local, smtp4dev) or "Ses" (AWS).</summary>
    public string Provider { get; set; } = "Smtp";

    public string FromAddress { get; set; } = "itsupport@topcashback.example";
    public string FromName { get; set; } = "TopCashback IT Support";

    public SmtpSettings Smtp { get; set; } = new();

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 2525;
        public bool UseStartTls { get; set; }

        /// <summary>Optional; supply through user secrets, never appsettings.</summary>
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}

/// <summary>"Dashboard": the Hangfire dashboard, always bound to localhost only.</summary>
public sealed class WorkerDashboardOptions
{
    public const string SectionName = "Dashboard";

    public int Port { get; set; } = 5280;
}

/// <summary>"CloudWatch": the heartbeat metric. Off unless enabled (it is off locally).</summary>
public sealed class CloudWatchOptions
{
    public const string SectionName = "CloudWatch";

    public bool Enabled { get; set; }
    public string Namespace { get; set; } = "TopSupport/Worker";
}

/// <summary>"Jobs": the recurring jobs' schedules (cron, in <see cref="TimeZone"/>) and limits.</summary>
public sealed class JobOptions
{
    public const string SectionName = "Jobs";

    /// <summary>IANA id; works on Windows and Linux.</summary>
    public string TimeZone { get; set; } = "Europe/London";

    public string TicketEventsCron { get; set; } = "* * * * *";
    public string SlaSweepCron { get; set; } = "*/5 * * * *";
    public string AutoCloseCron { get; set; } = "0 2 * * *";
    public string DailyDigestCron { get; set; } = "0 8 * * 1-5";
    public string HeartbeatCron { get; set; } = "*/5 * * * *";

    /// <summary>The first ticket-events run only looks this far back, so old history isn't emailed.</summary>
    public int FirstRunLookbackMinutes { get; set; } = 60;

    /// <summary>
    /// Each ticket-events run re-reads this much before the previous run's end, in case the Api's
    /// clock is slightly behind the worker's. NotificationLog stops those events being re-sent.
    /// </summary>
    public int EventOverlapMinutes { get; set; } = 5;

    public int AutoCloseAfterDays { get; set; } = 7;
}
