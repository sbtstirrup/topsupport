using TopSupport.Models.Enums;
using TopSupport.Worker.Data;
using TopSupport.Worker.Tests.Fakes;

namespace TopSupport.Worker.Tests;

public class DailyDigestJobTests
{
    private readonly WorkerTestContext _ctx = new();

    public DailyDigestJobTests()
    {
        _ctx.Store.DigestTeams.Add(new DigestTeam("t1", "Service Desk", "Ellie Shaw", "ellie.shaw@topcashback.example"));
        _ctx.Store.DigestTeams.Add(new DigestTeam("t2", "Infrastructure", "Nadia Hussain", "nadia.hussain@topcashback.example"));
    }

    private DigestTicket Ticket(string number, TicketPriority priority, TimeSpan age, string teamId = "t1", DateTimeOffset? firstReply = null)
        => new("T" + number, number, "Subject " + number, TicketStatus.Open, priority, _ctx.Time.GetUtcNow() - age, teamId, firstReply, "Jamie Moore");

    [Fact]
    public async Task Each_team_lead_gets_their_own_teams_tickets_with_overdue_ones_first()
    {
        _ctx.Store.DigestTickets.Add(Ticket("060001", TicketPriority.Normal, TimeSpan.FromHours(2)));
        _ctx.Store.DigestTickets.Add(Ticket("060002", TicketPriority.Urgent, TimeSpan.FromHours(5)));
        _ctx.Store.DigestTickets.Add(Ticket("060003", TicketPriority.Low, TimeSpan.FromHours(1), teamId: "t2"));

        await _ctx.DailyDigest().RunAsync(CancellationToken.None);

        var ellie = Assert.Single(_ctx.Email.Sent, e => e.To == "ellie.shaw@topcashback.example");
        Assert.Contains("2 open, 1 overdue", ellie.Subject);
        Assert.True(ellie.TextBody.IndexOf("#060002", StringComparison.Ordinal) < ellie.TextBody.IndexOf("#060001", StringComparison.Ordinal));
        Assert.Contains("#060002  Subject 060002  (Urgent, Open)  OVERDUE", ellie.TextBody);
        Assert.DoesNotContain("#060003", ellie.TextBody);

        var nadia = Assert.Single(_ctx.Email.Sent, e => e.To == "nadia.hussain@topcashback.example");
        Assert.Contains("1 open, 0 overdue", nadia.Subject);
    }

    [Fact]
    public async Task A_ticket_with_an_agent_reply_is_not_overdue()
    {
        _ctx.Store.DigestTickets.Add(Ticket("060001", TicketPriority.Urgent, TimeSpan.FromHours(5), firstReply: _ctx.Time.GetUtcNow().AddHours(-4)));

        await _ctx.DailyDigest().RunAsync(CancellationToken.None);

        Assert.Contains("1 open, 0 overdue", Assert.Single(_ctx.Email.Sent, e => e.To.StartsWith("ellie")).Subject);
    }

    [Fact]
    public async Task A_team_with_nothing_open_still_hears_so()
    {
        await _ctx.DailyDigest().RunAsync(CancellationToken.None);

        var email = Assert.Single(_ctx.Email.Sent, e => e.To.StartsWith("ellie"));
        Assert.Contains("There are no open tickets for your team.", email.TextBody);
    }

    [Fact]
    public async Task One_digest_per_team_per_London_day()
    {
        // 26 Oct 2026, 09:00 UTC is 09:00 in London (the clocks went back on the 25th).
        await _ctx.DailyDigest().RunAsync(CancellationToken.None);
        await _ctx.DailyDigest().RunAsync(CancellationToken.None);
        Assert.Equal(2, _ctx.Email.Sent.Count);
        Assert.Contains("Mon 26 Oct", _ctx.Email.Sent[0].Subject);

        _ctx.Time.Advance(TimeSpan.FromDays(1));
        await _ctx.DailyDigest().RunAsync(CancellationToken.None);

        Assert.Equal(4, _ctx.Email.Sent.Count);
        Assert.Contains("Tue 27 Oct", _ctx.Email.Sent[^1].Subject);
    }
}
