using System.Data;

namespace TopSupport.Worker.Data;

/// <summary>ADO.NET implementation of INotificationLog over worker.usp_NotificationLog_*.</summary>
public sealed class NotificationLog : INotificationLog
{
    private readonly WorkerDb _db;

    public NotificationLog(WorkerDb db)
    {
        _db = db;
    }

    public Task<bool> TryClaimAsync(string ticketId, string eventKey, string recipient, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        => _db.ScalarAsync<bool>("worker.usp_NotificationLog_TryClaim", p =>
        {
            p.AddWithValue("@TicketId", ticketId);
            p.AddWithValue("@EventKey", eventKey);
            p.AddWithValue("@Recipient", recipient);
            p.Add("@SentAtUtc", SqlDbType.DateTimeOffset).Value = sentAtUtc;
        }, cancellationToken);

    public Task ReleaseAsync(string ticketId, string eventKey, string recipient, CancellationToken cancellationToken)
        => _db.ExecuteAsync("worker.usp_NotificationLog_Release", p =>
        {
            p.AddWithValue("@TicketId", ticketId);
            p.AddWithValue("@EventKey", eventKey);
            p.AddWithValue("@Recipient", recipient);
        }, cancellationToken);
}
