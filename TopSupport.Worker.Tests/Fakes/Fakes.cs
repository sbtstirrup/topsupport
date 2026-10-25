using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;
using TopSupport.Worker.Jobs;
using TopSupport.Worker.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace TopSupport.Worker.Tests.Fakes;

/// <summary>In-memory IWorkerTicketStore: tests set the rows each proc would return.</summary>
public sealed class FakeTicketStore : IWorkerTicketStore
{
    public List<TicketEvent> Events { get; } = new();
    public List<SlaCandidate> SlaTickets { get; } = new();
    public List<TeamMember> TeamMembers { get; } = new();
    public List<ResolvedTicket> Resolved { get; } = new();
    public List<string> Closed { get; } = new();
    public List<(DateTimeOffset Since, DateTimeOffset Until)> EventQueries { get; } = new();

    public Task<IReadOnlyList<TicketEvent>> GetEventsAsync(DateTimeOffset sinceUtc, DateTimeOffset untilUtc, CancellationToken cancellationToken)
    {
        EventQueries.Add((sinceUtc, untilUtc));
        // Same window rule as worker.usp_TicketEvents_GetSince.
        return Task.FromResult<IReadOnlyList<TicketEvent>>(Events.Where(e => e.At > sinceUtc && e.At <= untilUtc).OrderBy(e => e.At).ToList());
    }

    public Task<(IReadOnlyList<SlaCandidate> Tickets, IReadOnlyList<TeamMember> TeamMembers)> GetSlaCandidatesAsync(CancellationToken cancellationToken)
        => Task.FromResult<(IReadOnlyList<SlaCandidate>, IReadOnlyList<TeamMember>)>((SlaTickets, TeamMembers));

    public Task<IReadOnlyList<ResolvedTicket>> GetResolvedTicketsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ResolvedTicket>>(Resolved.Where(r => !Closed.Contains(r.TicketId)).ToList());

    public Task<bool> CloseResolvedTicketAsync(string ticketId, DateTimeOffset atUtc, CancellationToken cancellationToken)
    {
        if (Closed.Contains(ticketId)) return Task.FromResult(false);
        Closed.Add(ticketId);
        return Task.FromResult(true);
    }

    public Task<(IReadOnlyList<DigestTeam> Teams, IReadOnlyList<DigestTicket> Tickets)> GetDigestAsync(CancellationToken cancellationToken)
        => Task.FromResult<(IReadOnlyList<DigestTeam>, IReadOnlyList<DigestTicket>)>((Array.Empty<DigestTeam>(), Array.Empty<DigestTicket>()));
}

/// <summary>In-memory worker.NotificationLog with the same unique (TicketId, EventKey, Recipient) rule.</summary>
public sealed class FakeNotificationLog : INotificationLog
{
    public HashSet<(string TicketId, string EventKey, string Recipient)> Rows { get; } = new();

    public Task<bool> TryClaimAsync(string ticketId, string eventKey, string recipient, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        => Task.FromResult(Rows.Add((ticketId, eventKey, recipient)));

    public Task ReleaseAsync(string ticketId, string eventKey, string recipient, CancellationToken cancellationToken)
    {
        Rows.Remove((ticketId, eventKey, recipient));
        return Task.CompletedTask;
    }
}

public sealed class FakeJobCursorStore : IJobCursorStore
{
    public Dictionary<string, DateTimeOffset> Cursors { get; } = new();

    public Task<DateTimeOffset?> GetAsync(string jobName, CancellationToken cancellationToken)
        => Task.FromResult<DateTimeOffset?>(Cursors.TryGetValue(jobName, out var c) ? c : null);

    public Task SetAsync(string jobName, DateTimeOffset lastCompletedUtc, CancellationToken cancellationToken)
    {
        Cursors[jobName] = lastCompletedUtc;
        return Task.CompletedTask;
    }
}

/// <summary>Records emails instead of sending them; can be told to fail the next N sends.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = new();
    public int FailNext { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailNext > 0)
        {
            FailNext--;
            throw new InvalidOperationException("SMTP is down (test).");
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>Wires the jobs to the fakes and a FakeTimeProvider.</summary>
public sealed class WorkerTestContext
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero));
    public FakeTicketStore Store { get; } = new();
    public FakeNotificationLog Log { get; } = new();
    public FakeJobCursorStore Cursors { get; } = new();
    public CapturingEmailSender Email { get; } = new();
    public JobOptions Jobs { get; } = new();
    public AppOptions App { get; } = new() { BaseUrl = "https://topsupport.test" };

    private NotificationDispatcher Dispatcher => new(Log, Email, Time);

    public TicketEventsJob TicketEvents()
        => new(Store, Cursors, Dispatcher, Time, MsOptions.Create(App), MsOptions.Create(Jobs), NullLogger<TicketEventsJob>.Instance);

    public SlaSweepJob SlaSweep()
        => new(Store, Dispatcher, Time, MsOptions.Create(App), NullLogger<SlaSweepJob>.Instance);

    public AutoCloseJob AutoClose()
        => new(Store, Time, MsOptions.Create(Jobs), NullLogger<AutoCloseJob>.Instance);
}
