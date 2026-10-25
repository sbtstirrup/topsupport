namespace TopSupport.Worker.Email;

/// <summary>One email: an HTML body with a plain-text fallback.</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>
/// Sends an email. Chosen by "Email:Provider": SmtpEmailSender (local, smtp4dev) or
/// SesEmailSender (AWS SES v2 with the EC2 instance role's credentials).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
