using TopSupport.Worker.Data;

namespace TopSupport.Worker.Email;

/// <summary>
/// Sends an email at most once per (ticket, event key, recipient): the NotificationLog row is
/// claimed before sending, so a retry or an overlapping run skips it, and released again if the
/// send fails, so Hangfire's retry sends it.
/// </summary>
public sealed class NotificationDispatcher
{
    private readonly INotificationLog _log;
    private readonly IEmailSender _sender;
    private readonly TimeProvider _time;

    public NotificationDispatcher(INotificationLog log, IEmailSender sender, TimeProvider time)
    {
        _log = log;
        _sender = sender;
        _time = time;
    }

    /// <summary>True when the email was sent now; false when it had already been sent.</summary>
    public async Task<bool> SendOnceAsync(string ticketId, string eventKey, EmailMessage message, CancellationToken cancellationToken)
    {
        var recipient = message.To.Trim().ToLowerInvariant();
        if (!await _log.TryClaimAsync(ticketId, eventKey, recipient, _time.GetUtcNow(), cancellationToken)) return false;

        try
        {
            await _sender.SendAsync(message, cancellationToken);
            return true;
        }
        catch
        {
            await _log.ReleaseAsync(ticketId, eventKey, recipient, CancellationToken.None);
            throw;
        }
    }
}
