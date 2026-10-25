using TopSupport.Models.Enums;
using TopSupport.Worker.Data;
using TopSupport.Worker.Jobs;
using TopSupport.Worker.Tests.Fakes;

namespace TopSupport.Worker.Tests;

public class TicketEventsJobTests
{
    private readonly WorkerTestContext _ctx = new();

    private TicketEvent Event(
        TicketEventType type, string eventId, TimeSpan ago,
        string? text = null, TicketStatus status = TicketStatus.Open,
        string? agentName = "Jamie Moore", string? agentEmail = "jamie.moore@topcashback.example")
        => new(type, eventId, _ctx.Time.GetUtcNow() - ago, text, type == TicketEventType.Reply ? "<p>We're on it.</p>" : null,
            "T060040", "060040", "Laptop will not charge", status,
            "Amira Patel", "amira.patel@topcashback.example", "a1", agentName, agentEmail);

    [Fact]
    public async Task First_run_only_emails_events_from_the_last_hour()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Created, "T060040", TimeSpan.FromDays(3)));
        _ctx.Store.Events.Add(Event(TicketEventType.Reply, "e1", TimeSpan.FromHours(2)));
        _ctx.Store.Events.Add(Event(TicketEventType.Reply, "e2", TimeSpan.FromMinutes(20)));

        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        var email = Assert.Single(_ctx.Email.Sent);
        Assert.Contains("New reply", email.Subject);
        Assert.Equal(_ctx.Time.GetUtcNow().AddMinutes(-60), _ctx.Store.EventQueries.Single().Since);
    }

    [Fact]
    public async Task Retry_after_a_failure_does_not_resend_what_was_already_sent()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Created, "T060040", TimeSpan.FromMinutes(3)));
        _ctx.Store.Events.Add(Event(TicketEventType.Reply, "e1", TimeSpan.FromMinutes(2)));
        _ctx.Store.Events.Add(Event(TicketEventType.Resolved, "e2", TimeSpan.FromMinutes(1), "Closed by J. Moore with status Resolved", TicketStatus.Resolved));
        var sender = _ctx.Email;

        // The created email goes out, then the reply send fails: Hangfire would retry the job.
        await RunWithFailureOnSecondSend();
        Assert.Single(sender.Sent);
        Assert.False(_ctx.Cursors.Cursors.ContainsKey(TicketEventsJob.JobName));

        _ctx.Time.Advance(TimeSpan.FromSeconds(30));
        await _ctx.TicketEvents().RunAsync(CancellationToken.None);
        _ctx.Time.Advance(TimeSpan.FromMinutes(1));
        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        Assert.Equal(3, sender.Sent.Count);
        Assert.Single(sender.Sent, e => e.Subject.Contains("received your ticket"));
        Assert.Single(sender.Sent, e => e.Subject.Contains("New reply"));
        Assert.Single(sender.Sent, e => e.Subject.Contains("resolved"));
    }

    [Fact]
    public async Task Overlapping_windows_do_not_resend()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Created, "T060040", TimeSpan.FromSeconds(30)));

        await _ctx.TicketEvents().RunAsync(CancellationToken.None);
        _ctx.Time.Advance(TimeSpan.FromMinutes(1));
        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        Assert.Single(_ctx.Email.Sent);
        // The second window re-read the first run's last five minutes (the clock-skew overlap).
        Assert.Equal(_ctx.Store.EventQueries[0].Until.AddMinutes(-5), _ctx.Store.EventQueries[1].Since);
    }

    [Fact]
    public async Task Assignment_emails_the_agent_named_by_the_event_only()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Assigned, "e1", TimeSpan.FromMinutes(3), "Assigned to Service Desk"));
        _ctx.Store.Events.Add(Event(TicketEventType.Assigned, "e2", TimeSpan.FromMinutes(2), "Assigned to E. Shaw"));
        _ctx.Store.Events.Add(Event(TicketEventType.Assigned, "e3", TimeSpan.FromMinutes(1), "Assigned to J. Moore"));

        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        var email = Assert.Single(_ctx.Email.Sent);
        Assert.Equal("jamie.moore@topcashback.example", email.To);
        Assert.Contains("https://topsupport.test/agent-tickets/T060040", email.TextBody);
    }

    [Fact]
    public async Task Resolved_event_is_skipped_once_the_ticket_has_been_reopened()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Resolved, "e1", TimeSpan.FromMinutes(2), "Closed by J. Moore with status Resolved", TicketStatus.Open));

        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        Assert.Empty(_ctx.Email.Sent);
    }

    [Fact]
    public async Task Reply_email_goes_to_the_requester_with_a_user_link()
    {
        _ctx.Store.Events.Add(Event(TicketEventType.Reply, "e1", TimeSpan.FromMinutes(1)));

        await _ctx.TicketEvents().RunAsync(CancellationToken.None);

        var email = Assert.Single(_ctx.Email.Sent);
        Assert.Equal("amira.patel@topcashback.example", email.To);
        Assert.Contains("We're on it.", email.TextBody);
        Assert.Contains("https://topsupport.test/tickets/T060040", email.TextBody);
    }

    [Theory]
    [InlineData("Jamie Moore", "J. Moore")]
    [InlineData("  Nadia   Hussain ", "N. Hussain")]
    [InlineData("Cher", "Cher")]
    public void ShortName_matches_TicketService(string name, string expected)
        => Assert.Equal(expected, TicketEventsJob.ShortName(name));

    private async Task RunWithFailureOnSecondSend()
    {
        var failing = new FailOnSecondSend(_ctx.Email);
        var job = new TicketEventsJob(
            _ctx.Store, _ctx.Cursors, new Email.NotificationDispatcher(_ctx.Log, failing, _ctx.Time), _ctx.Time,
            Microsoft.Extensions.Options.Options.Create(_ctx.App), Microsoft.Extensions.Options.Options.Create(_ctx.Jobs),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TicketEventsJob>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.RunAsync(CancellationToken.None));
    }

    /// <summary>Lets the first email through to the capturing sender, then fails.</summary>
    private sealed class FailOnSecondSend : Email.IEmailSender
    {
        private readonly CapturingEmailSender _inner;
        private int _calls;

        public FailOnSecondSend(CapturingEmailSender inner) => _inner = inner;

        public Task SendAsync(Email.EmailMessage message, CancellationToken cancellationToken)
            => ++_calls == 2 ? throw new InvalidOperationException("SMTP is down (test).") : _inner.SendAsync(message, cancellationToken);
    }
}
