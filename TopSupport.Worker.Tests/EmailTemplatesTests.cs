using TopSupport.Models.Enums;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;

namespace TopSupport.Worker.Tests;

public class EmailTemplatesTests
{
    private const string Link = "https://topsupport.test/tickets/T1";

    private static TicketEvent Event(string subject = "Printer jam", string requester = "Owen Pryce", string? replyHtml = null)
        => new(TicketEventType.Reply, "e1", DateTimeOffset.UtcNow, null, replyHtml, "T1", "060001", subject, TicketStatus.Open,
            requester, "owen.pryce@topcashback.example", "a1", "Jamie Moore", "jamie.moore@topcashback.example");

    [Fact]
    public void Ticket_text_is_html_encoded_in_the_html_body_and_plain_in_the_text_body()
    {
        var email = EmailTemplates.TicketCreated(Event(subject: "<b>Broken</b> & \"urgent\"", requester: "<script>Owen</script> Pryce"), Link);

        Assert.Contains("&lt;b&gt;Broken&lt;/b&gt; &amp; &quot;urgent&quot;", email.HtmlBody);
        Assert.Contains("Hi &lt;script&gt;Owen&lt;/script&gt;,", email.HtmlBody);
        Assert.DoesNotContain("<b>Broken</b>", email.HtmlBody);
        Assert.DoesNotContain("<script>", email.HtmlBody);
        Assert.Contains("\"<b>Broken</b> & \"urgent\"\"", email.TextBody);
        Assert.Equal("[#060001] We've received your ticket: <b>Broken</b> & \"urgent\"", email.Subject);
    }

    [Fact]
    public void Reply_html_is_sanitized_before_it_goes_in_the_email()
    {
        var email = EmailTemplates.AgentReply(Event(replyHtml: "<p>Try <strong>this</strong></p><img src=x onerror=alert(1)><script>alert(2)</script><a href=\"javascript:alert(3)\">x</a>"), Link);

        Assert.Contains("<p>Try <strong>this</strong></p>", email.HtmlBody);
        Assert.DoesNotContain("onerror", email.HtmlBody);
        Assert.DoesNotContain("<script", email.HtmlBody);
        Assert.DoesNotContain("javascript:", email.HtmlBody);
        Assert.Contains("Try this", email.TextBody);
    }

    [Fact]
    public void Resolved_email_states_the_configured_auto_close_delay()
    {
        var email = EmailTemplates.Resolved(Event(), Link, autoCloseAfterDays: 14);

        Assert.Contains("close automatically after 14 days", email.TextBody);
    }

    [Fact]
    public void Breach_time_is_shown_in_the_configured_time_zone()
    {
        var ticket = new SlaCandidate("T1", "060001", "Printer jam", TicketStatus.Open, TicketPriority.Urgent,
            new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero), null, "Jamie Moore", "jamie.moore@topcashback.example", "t1", "Service Desk");
        var due = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

        var email = EmailTemplates.SlaBreach(ticket, "jamie.moore@topcashback.example", "Jamie Moore", due, Link, TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));

        // British Summer Time: 12:00 UTC is 13:00 in London.
        Assert.Contains("01/07/2026, 13:00", email.TextBody);
    }

    [Fact]
    public void Every_email_links_to_the_ticket()
    {
        Assert.Contains($"href=\"{Link}\"", EmailTemplates.TicketCreated(Event(), Link).HtmlBody);
        Assert.Contains(Link, EmailTemplates.TicketCreated(Event(), Link).TextBody);
    }
}
