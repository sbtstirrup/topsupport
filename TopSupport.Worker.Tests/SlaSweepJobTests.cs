using TopSupport.Models.Enums;
using TopSupport.Worker.Data;
using TopSupport.Worker.Tests.Fakes;

namespace TopSupport.Worker.Tests;

public class SlaSweepJobTests
{
    private readonly WorkerTestContext _ctx = new();

    private SlaCandidate Ticket(
        string id, TicketPriority priority, TimeSpan age,
        TicketStatus status = TicketStatus.Open, DateTimeOffset? firstReply = null,
        string? agentEmail = "jamie.moore@topcashback.example", string? teamId = "t1")
        => new(id, id[1..], "Laptop will not charge", status, priority, _ctx.Time.GetUtcNow() - age, firstReply,
            agentEmail is null ? null : "Jamie Moore", agentEmail, teamId, "Service Desk");

    [Fact]
    public async Task Breach_emails_the_agent_once_and_never_twice()
    {
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.Urgent, TimeSpan.FromHours(5)));

        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        _ctx.Time.Advance(TimeSpan.FromMinutes(5));
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        _ctx.Time.Advance(TimeSpan.FromHours(3));
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);

        var email = Assert.Single(_ctx.Email.Sent);
        Assert.Equal("jamie.moore@topcashback.example", email.To);
        Assert.Contains("Response target missed", email.Subject);
        Assert.Contains("https://topsupport.test/agent-tickets/T1", email.TextBody);
    }

    [Fact]
    public async Task Ticket_still_inside_its_target_is_not_emailed_until_it_passes()
    {
        // Normal = 24h.
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.Normal, TimeSpan.FromHours(23)));

        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        Assert.Empty(_ctx.Email.Sent);

        _ctx.Time.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        Assert.Single(_ctx.Email.Sent);
    }

    [Fact]
    public async Task Tickets_with_an_agent_response_are_ignored()
    {
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.High, TimeSpan.FromHours(30), firstReply: _ctx.Time.GetUtcNow().AddHours(-29)));

        await _ctx.SlaSweep().RunAsync(CancellationToken.None);

        Assert.Empty(_ctx.Email.Sent);
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public async Task Resolved_and_closed_tickets_are_ignored(TicketStatus status)
    {
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.Urgent, TimeSpan.FromDays(3), status));

        await _ctx.SlaSweep().RunAsync(CancellationToken.None);

        Assert.Empty(_ctx.Email.Sent);
    }

    [Fact]
    public async Task Ticket_with_no_agent_emails_each_team_member_once()
    {
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.Low, TimeSpan.FromHours(80), agentEmail: null));
        _ctx.Store.TeamMembers.Add(new TeamMember("t1", "a1", "Jamie Moore", "jamie.moore@topcashback.example"));
        _ctx.Store.TeamMembers.Add(new TeamMember("t1", "a3", "Ellie Shaw", "ellie.shaw@topcashback.example"));
        _ctx.Store.TeamMembers.Add(new TeamMember("t2", "a4", "Callum Reid", "callum.reid@topcashback.example"));

        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);

        Assert.Equal(
            new[] { "ellie.shaw@topcashback.example", "jamie.moore@topcashback.example" },
            _ctx.Email.Sent.Select(e => e.To).Order());
    }

    [Fact]
    public async Task Failed_send_is_retried_on_the_next_run_and_then_sent_once()
    {
        _ctx.Store.SlaTickets.Add(Ticket("T1", TicketPriority.Urgent, TimeSpan.FromHours(5)));
        _ctx.Email.FailNext = 1;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.SlaSweep().RunAsync(CancellationToken.None));
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);
        await _ctx.SlaSweep().RunAsync(CancellationToken.None);

        Assert.Single(_ctx.Email.Sent);
    }
}
