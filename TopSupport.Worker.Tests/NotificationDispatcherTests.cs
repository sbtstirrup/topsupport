using TopSupport.Worker.Email;
using TopSupport.Worker.Tests.Fakes;

namespace TopSupport.Worker.Tests;

public class NotificationDispatcherTests
{
    private readonly WorkerTestContext _ctx = new();

    private static EmailMessage To(string address) => new(address, "Subject", "<p>Hi</p>", "Hi");

    [Fact]
    public async Task The_same_email_is_sent_once_however_the_address_is_written()
    {
        var dispatcher = _ctx.Dispatcher;

        Assert.True(await dispatcher.SendOnceAsync("T1", "created", To("Owen.Pryce@topcashback.example"), CancellationToken.None));
        Assert.False(await dispatcher.SendOnceAsync("T1", "created", To(" owen.pryce@topcashback.example "), CancellationToken.None));

        Assert.Single(_ctx.Email.Sent);
        Assert.Contains(("T1", "created", "owen.pryce@topcashback.example"), _ctx.Log.Rows);
    }

    [Fact]
    public async Task Different_events_and_recipients_are_separate_emails()
    {
        var dispatcher = _ctx.Dispatcher;

        await dispatcher.SendOnceAsync("T1", "created", To("a@x.example"), CancellationToken.None);
        await dispatcher.SendOnceAsync("T1", "reply:e1", To("a@x.example"), CancellationToken.None);
        await dispatcher.SendOnceAsync("T1", "sla-breach", To("a@x.example"), CancellationToken.None);
        await dispatcher.SendOnceAsync("T1", "sla-breach", To("b@x.example"), CancellationToken.None);

        Assert.Equal(4, _ctx.Email.Sent.Count);
    }

    [Fact]
    public async Task A_failed_send_releases_its_claim_and_rethrows_so_the_retry_sends_it()
    {
        _ctx.Email.FailNext = 1;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.Dispatcher.SendOnceAsync("T1", "created", To("a@x.example"), CancellationToken.None));
        Assert.Empty(_ctx.Log.Rows);

        Assert.True(await _ctx.Dispatcher.SendOnceAsync("T1", "created", To("a@x.example"), CancellationToken.None));
        Assert.Single(_ctx.Email.Sent);
    }
}
