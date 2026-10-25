using Hangfire;
using Microsoft.Extensions.Options;
using TopSupport.Services.Tickets;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Jobs;

/// <summary>
/// "sla-sweep" (every 5 minutes): an active ticket (Open, In Progress, Awaiting User) with no
/// public agent reply past its first-response target (TicketSla.SlaHours: Urgent 4h, High 8h,
/// Normal 24h, Low 72h from Created) emails its agent, or every active member of its team when
/// no agent has it. Once per ticket and recipient (event key "sla-breach").
/// </summary>
public sealed class SlaSweepJob
{
    public const string JobName = "sla-sweep";
    public const string EventKey = "sla-breach";

    private readonly IWorkerTicketStore _store;
    private readonly NotificationDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly AppOptions _app;
    private readonly ILogger<SlaSweepJob> _logger;

    public SlaSweepJob(IWorkerTicketStore store, NotificationDispatcher dispatcher, TimeProvider time, IOptions<AppOptions> app, ILogger<SlaSweepJob> logger)
    {
        _store = store;
        _dispatcher = dispatcher;
        _time = time;
        _app = app.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var (tickets, members) = await _store.GetSlaCandidatesAsync(cancellationToken);
        var membersByTeam = members.ToLookup(m => m.TeamId);

        var sent = 0;
        foreach (var t in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The proc already filters these; checked again so the rule lives (and is tested) here.
            if (!TicketSla.ActiveStatuses.Contains(t.Status) || t.FirstAgentReplyAt is not null) continue;
            var due = t.Created.AddHours(TicketSla.SlaHours[t.Priority]);
            if (now <= due) continue;

            var recipients = !string.IsNullOrWhiteSpace(t.AgentEmail)
                ? new[] { (Name: t.AgentName ?? string.Empty, Email: t.AgentEmail) }
                : membersByTeam[t.TeamId ?? string.Empty].Select(m => (m.Name, m.Email)).ToArray();

            foreach (var (name, email) in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
            {
                var message = EmailTemplates.SlaBreach(t, email, name, due, _app.AgentTicketUrl(t.TicketId));
                if (await _dispatcher.SendOnceAsync(t.TicketId, EventKey, message, cancellationToken)) sent++;
            }
        }

        _logger.LogInformation("sla-sweep: {Sent} breach email(s) sent.", sent);
    }
}
