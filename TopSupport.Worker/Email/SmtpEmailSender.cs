using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Email;

/// <summary>SMTP via MailKit. Locally it points at smtp4dev (see docker-compose.worker.yml).</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        var smtp = _options.Smtp;
        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
