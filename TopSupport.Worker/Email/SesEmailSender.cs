using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using TopSupport.Worker.Options;

namespace TopSupport.Worker.Email;

/// <summary>
/// AWS SES v2. The client is created with the SDK's default credential and region chain, which
/// on EC2 is the instance role: no access keys in configuration, ever.
/// </summary>
public sealed class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly EmailOptions _options;

    public SesEmailSender(IAmazonSimpleEmailServiceV2 ses, IOptions<EmailOptions> options)
    {
        _ses = ses;
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        await _ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = $"{_options.FromName} <{_options.FromAddress}>",
            Destination = new Destination { ToAddresses = new List<string> { message.To } },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = message.Subject, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Html = new Content { Data = message.HtmlBody, Charset = "UTF-8" },
                        Text = new Content { Data = message.TextBody, Charset = "UTF-8" }
                    }
                }
            }
        }, cancellationToken);
    }
}
