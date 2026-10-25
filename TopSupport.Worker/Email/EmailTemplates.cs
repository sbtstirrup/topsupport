using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TopSupport.Models.Enums;
using TopSupport.Services.Tickets;
using TopSupport.Worker.Data;

namespace TopSupport.Worker.Email;

/// <summary>
/// The worker's emails: each an HTML body (in the green TopSupport style, every value
/// HTML-encoded) plus a plain-text fallback, and always a link to the ticket.
/// </summary>
public static partial class EmailTemplates
{
    public static EmailMessage TicketCreated(TicketEvent e, string link)
        => Build(e.RequesterEmail, $"[#{e.Number}] We've received your ticket: {e.Subject}",
            $"Hi {FirstName(e.RequesterName)},",
            new[] { $"Thanks for contacting IT Support. Your ticket #{e.Number} \"{e.Subject}\" is logged and the team will pick it up." },
            null, "View your ticket", link);

    public static EmailMessage AgentReply(TicketEvent e, string link)
        => Build(e.RequesterEmail, $"[#{e.Number}] New reply on your ticket: {e.Subject}",
            $"Hi {FirstName(e.RequesterName)},",
            new[] { $"The IT team has replied to ticket #{e.Number}:" },
            e.ReplyHtml is null ? null : TicketHtmlSanitizer.Sanitize(e.ReplyHtml), "View and reply", link);

    public static EmailMessage Assigned(TicketEvent e, string link)
        => Build(e.AgentEmail!, $"[#{e.Number}] Assigned to you: {e.Subject}",
            $"Hi {FirstName(e.AgentName)},",
            new[] { $"Ticket #{e.Number} \"{e.Subject}\" from {e.RequesterName} has been assigned to you." },
            null, "Open the ticket", link);

    public static EmailMessage Resolved(TicketEvent e, string link, int autoCloseAfterDays)
        => Build(e.RequesterEmail, $"[#{e.Number}] Your ticket has been resolved: {e.Subject}",
            $"Hi {FirstName(e.RequesterName)},",
            new[]
            {
                $"Ticket #{e.Number} \"{e.Subject}\" has been marked as resolved.",
                $"If the problem isn't fixed, reply on the ticket to reopen it. Resolved tickets close automatically after {autoCloseAfterDays} days."
            },
            null, "View your ticket", link);

    public static EmailMessage SlaBreach(SlaCandidate t, string to, string recipientName, DateTimeOffset due, string link, TimeZoneInfo timeZone)
        => Build(to, $"[#{t.Number}] Response target missed: {t.Subject}",
            $"Hi {FirstName(recipientName)},",
            new[]
            {
                $"Ticket #{t.Number} \"{t.Subject}\" ({t.Priority} priority) has had no reply from the IT team and passed its response target at {FormatLocal(due, timeZone)}.",
                t.AgentName is null && t.TeamName is not null ? $"It is assigned to the {t.TeamName} team with no agent yet." : "Please reply to the requester as soon as you can."
            },
            null, "Open the ticket", link);

    public static EmailMessage Digest(DigestTeam team, IReadOnlyList<(DigestTicket Ticket, bool Overdue, string Link)> tickets, DateOnly date)
    {
        var overdue = tickets.Count(t => t.Overdue);
        var html = new StringBuilder();
        var text = new StringBuilder();
        if (tickets.Count == 0)
        {
            html.Append("<p>There are no open tickets for your team.</p>");
            text.AppendLine("There are no open tickets for your team.");
        }
        else
        {
            html.Append("<table style=\"border-collapse:collapse;width:100%;font-size:14px\"><tr>")
                .Append(Th("Ticket")).Append(Th("Subject")).Append(Th("Priority")).Append(Th("Status")).Append(Th("Assigned to")).Append("</tr>");
            foreach (var (t, isOverdue, link) in tickets)
            {
                html.Append("<tr>")
                    .Append(Td($"<a href=\"{Enc(link)}\" style=\"color:#1b8463\">#{Enc(t.Number)}</a>"))
                    .Append(Td(Enc(t.Subject) + (isOverdue ? " <strong style=\"color:#b42318\">Overdue</strong>" : "")))
                    .Append(Td(Enc(t.Priority.ToString())))
                    .Append(Td(Enc(TicketStatusText.Label(t.Status))))
                    .Append(Td(Enc(t.AgentName ?? team.TeamName)))
                    .Append("</tr>");
                text.AppendLine($"#{t.Number}  {t.Subject}  ({t.Priority}, {TicketStatusText.Label(t.Status)}){(isOverdue ? "  OVERDUE" : "")}  {link}");
            }

            html.Append("</table>");
        }

        var intro = $"Your team has {tickets.Count} open {(tickets.Count == 1 ? "ticket" : "tickets")}, {overdue} overdue.";
        return Build(team.LeadEmail, $"{team.TeamName} daily digest for {date:ddd d MMM}: {tickets.Count} open, {overdue} overdue",
            $"Hi {FirstName(team.LeadName)},", new[] { intro }, null, null, null, html.ToString(), text.ToString());
    }

    private static EmailMessage Build(
        string to, string subject, string greeting, IEnumerable<string> paragraphs,
        string? quotedHtml, string? buttonText, string? link,
        string? extraHtml = null, string? extraText = null)
    {
        var html = new StringBuilder()
            .Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#1f2a2e;max-width:640px\">")
            .Append("<div style=\"background:#1b8463;color:#fff;padding:14px 20px;font-weight:700;font-size:16px\">TopSupport</div>")
            .Append("<div style=\"padding:20px\">")
            .Append($"<p>{Enc(greeting)}</p>");
        var text = new StringBuilder().AppendLine(greeting).AppendLine();

        foreach (var p in paragraphs)
        {
            html.Append($"<p>{Enc(p)}</p>");
            text.AppendLine(p).AppendLine();
        }

        if (quotedHtml is not null)
        {
            // Sanitized again by AgentReply (the Api sanitizes before storing; rows written any other way aren't).
            html.Append($"<blockquote style=\"border-left:3px solid #1b8463;margin:0 0 16px;padding:4px 14px;color:#33464d\">{quotedHtml}</blockquote>");
            text.AppendLine(ToPlainText(quotedHtml)).AppendLine();
        }

        if (extraHtml is not null)
        {
            html.Append(extraHtml);
            text.AppendLine(extraText);
        }

        if (buttonText is not null && link is not null)
        {
            html.Append($"<p><a href=\"{Enc(link)}\" style=\"display:inline-block;background:#1b8463;color:#fff;text-decoration:none;padding:10px 18px;border-radius:999px;font-weight:600\">{Enc(buttonText)}</a></p>");
            text.AppendLine($"{buttonText}: {link}").AppendLine();
        }

        html.Append("<p style=\"color:#5b6b70;font-size:12px\">TopCashback IT Support &middot; itsupport@topcashback.example &middot; ext. 4400</p></div></div>");
        text.AppendLine("TopCashback IT Support - itsupport@topcashback.example - ext. 4400");
        return new EmailMessage(to, subject, html.ToString(), text.ToString());
    }

    private static string Th(string s) => $"<th style=\"text-align:left;border-bottom:2px solid #1b8463;padding:6px\">{s}</th>";

    private static string Td(string html) => $"<td style=\"border-bottom:1px solid #e3e7e8;padding:6px;vertical-align:top\">{html}</td>";

    private static string Enc(string s) => WebUtility.HtmlEncode(s);

    private static string FirstName(string? name) => string.IsNullOrWhiteSpace(name) ? "there" : name.Trim().Split(' ')[0];

    /// <summary>In the jobs' time zone ("Jobs:TimeZone"), the way the UK app shows dates.</summary>
    private static string FormatLocal(DateTimeOffset utc, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTime(utc, timeZone).ToString("dd/MM/yyyy, HH:mm");

    private static string ToPlainText(string html)
        => WebUtility.HtmlDecode(Tags().Replace(BlockBreaks().Replace(html, "\n"), string.Empty)).Trim();

    [GeneratedRegex(@"<\s*(br|/p|/div|/li)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreaks();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}
