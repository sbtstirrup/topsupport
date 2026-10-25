using Hangfire;
using Microsoft.Extensions.Options;
using TopSupport.Models.Enums;
using TopSupport.Models.People;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// "ticket-events" (every minute): emails what happened on tickets since the last run.
/// - created  → requester
/// - agent public reply → requester (internal notes are never read: they're Kind = 'Note')
/// - assigned → the assigned agent, only when the "Assigned to J. Moore" event names the
///   ticket's current agent (team-only assignments and stale reassignments are skipped)
/// - resolved → requester, only while the ticket is still Resolved
/// The window starts at the previous run's end minus a small overlap (dedupe makes the overlap
/// harmless) or, on the very first run, an hour ago, so existing history isn't emailed.
/// </summary>
public sealed class TicketEventsJob
{
    public const string JobName = "ticket-events";

    private readonly IWorkerTicketStore _store;
    private readonly IJobCursorStore _cursors;
    private readonly NotificationDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly AppOptions _app;
    private readonly JobOptions _jobs;
    private readonly ILogger<TicketEventsJob> _logger;

    public TicketEventsJob(
        IWorkerTicketStore store, IJobCursorStore cursors, NotificationDispatcher dispatcher, TimeProvider time,
        IOptions<AppOptions> app, IOptions<JobOptions> jobs, ILogger<TicketEventsJob> logger)
    {
        _store = store;
        _cursors = cursors;
        _dispatcher = dispatcher;
        _time = time;
        _app = app.Value;
        _jobs = jobs.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var cursor = await _cursors.GetAsync(JobName, cancellationToken);
        var since = cursor is { } last
            ? last.AddMinutes(-_jobs.EventOverlapMinutes)
            : now.AddMinutes(-_jobs.FirstRunLookbackMinutes);

        var sent = 0;
        foreach (var e in await _store.GetEventsAsync(since, now, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Compose(e) is { } email && await _dispatcher.SendOnceAsync(e.TicketId, email.EventKey, email.Message, cancellationToken))
            {
                sent++;
            }
        }

        // Only after every email went out: a failure leaves the cursor, so the retry re-reads the window.
        await _cursors.SetAsync(JobName, now, cancellationToken);
        _logger.LogInformation("ticket-events: {Sent} email(s) sent for events after {Since:u}.", sent, since);
    }

    private (string EventKey, EmailMessage Message)? Compose(TicketEvent e)
    {
        switch (e.Type)
        {
            case TicketEventType.Created when HasAddress(e.RequesterEmail):
                return ("created", EmailTemplates.TicketCreated(e, _app.UserTicketUrl(e.TicketId)));

            case TicketEventType.Reply when HasAddress(e.RequesterEmail):
                return ($"reply:{e.EventId}", EmailTemplates.AgentReply(e, _app.UserTicketUrl(e.TicketId)));

            case TicketEventType.Assigned when HasAddress(e.AgentEmail) && NamesCurrentAgent(e):
                return ($"assigned:{e.EventId}", EmailTemplates.Assigned(e, _app.AgentTicketUrl(e.TicketId)));

            case TicketEventType.Resolved when e.Status == TicketStatus.Resolved && HasAddress(e.RequesterEmail):
                return ($"resolved:{e.EventId}", EmailTemplates.Resolved(e, _app.UserTicketUrl(e.TicketId), _jobs.AutoCloseAfterDays));

            default:
                return null;
        }
    }

    /// <summary>"Assigned to J. Moore" (TicketService writes PersonName.Short(agent.Name)) for the current agent.</summary>
    internal static bool NamesCurrentAgent(TicketEvent e)
        => e.AgentName is not null && e.EventText == "Assigned to " + PersonName.Short(e.AgentName);

    private static bool HasAddress(string? email) => !string.IsNullOrWhiteSpace(email);
}
