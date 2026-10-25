using TopSupport.Worker.Data;
using TopSupport.Worker.Tests.Fakes;

namespace TopSupport.Worker.Tests;

public class AutoCloseJobTests
{
    private readonly WorkerTestContext _ctx = new();

    [Fact]
    public async Task Closes_only_tickets_resolved_seven_or_more_days_ago()
    {
        var now = _ctx.Time.GetUtcNow();
        _ctx.Store.Resolved.Add(new ResolvedTicket("T1", "000001", now.AddDays(-7)));
        _ctx.Store.Resolved.Add(new ResolvedTicket("T2", "000002", now.AddDays(-12)));
        _ctx.Store.Resolved.Add(new ResolvedTicket("T3", "000003", now.AddDays(-7).AddMinutes(1)));
        _ctx.Store.Resolved.Add(new ResolvedTicket("T4", "000004", now.AddHours(-2)));

        await _ctx.AutoClose().RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "T1", "T2" }, _ctx.Store.Closed.Order());
    }

    [Fact]
    public async Task Ticket_closes_on_the_first_run_after_its_seventh_day()
    {
        _ctx.Store.Resolved.Add(new ResolvedTicket("T1", "000001", _ctx.Time.GetUtcNow().AddDays(-6)));

        await _ctx.AutoClose().RunAsync(CancellationToken.None);
        Assert.Empty(_ctx.Store.Closed);

        _ctx.Time.Advance(TimeSpan.FromDays(1));
        await _ctx.AutoClose().RunAsync(CancellationToken.None);
        Assert.Equal(new[] { "T1" }, _ctx.Store.Closed);
    }
}
