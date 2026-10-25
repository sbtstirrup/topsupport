using Hangfire;
using Microsoft.Extensions.Options;
using TopSupport.Services.Tickets;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// "daily-digest" (weekdays, 08:00 Europe/London): each team lead (Teams.LeadAgentId) gets their
/// team's active tickets, with the overdue ones flagged by the same rule as the queue's Overdue
/// view (no agent reply and past TicketSla's target). Once per team and London date.
/// </summary>
public sealed class DailyDigestJob
{
    public const string JobName = "daily-digest";

    private readonly IWorkerTicketStore _store;
    private readonly NotificationDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly AppOptions _app;
    private readonly JobOptions _jobs;
    private readonly ILogger<DailyDigestJob> _logger;

    public DailyDigestJob(
        IWorkerTicketStore store, NotificationDispatcher dispatcher, TimeProvider time,
        IOptions<AppOptions> app, IOptions<JobOptions> jobs, ILogger<DailyDigestJob> logger)
    {
        _store = store;
        _dispatcher = dispatcher;
        _time = time;
        _app = app.Value;
        _jobs = jobs.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(_jobs.TimeZone)).DateTime);
        var (teams, tickets) = await _store.GetDigestAsync(cancellationToken);
        var ticketsByTeam = tickets.ToLookup(t => t.TeamId);

        var sent = 0;
        foreach (var team in teams)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = ticketsByTeam[team.TeamId]
                .Select(t => (Ticket: t, Overdue: TicketSla.IsOverdue(t.Status, t.FirstAgentReplyAt, t.Created, t.Priority, now), Link: _app.AgentTicketUrl(t.TicketId)))
                .OrderByDescending(r => r.Overdue)
                .ThenBy(r => r.Ticket.Created)
                .ToList();

            // Not about one ticket, so the log row's TicketId is empty and the key carries team and date.
            var message = EmailTemplates.Digest(team, rows, today);
            if (await _dispatcher.SendOnceAsync(string.Empty, $"digest:{team.TeamId}:{today:yyyy-MM-dd}", message, cancellationToken)) sent++;
        }

        _logger.LogInformation("daily-digest: {Sent} digest(s) sent for {Date}.", sent, today);
    }
}
