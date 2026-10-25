namespace TopSupport.Worker.Data;

/// <summary>
/// Everything the jobs read from (and, for auto-close, write to) the TopSupport tables. The SQL
/// lives in worker.* stored procedures (TopSupport.Database/Scripts/15_Worker.sql).
/// </summary>
public interface IWorkerTicketStore
{
    /// <summary>Emailable events with <paramref name="sinceUtc"/> &lt; At &lt;= <paramref name="untilUtc"/>, oldest first.</summary>
    Task<IReadOnlyList<TicketEvent>> GetEventsAsync(DateTimeOffset sinceUtc, DateTimeOffset untilUtc, CancellationToken cancellationToken);

    /// <summary>Active tickets with no public agent reply yet, plus every team's active members.</summary>
    Task<(IReadOnlyList<SlaCandidate> Tickets, IReadOnlyList<TeamMember> TeamMembers)> GetSlaCandidatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ResolvedTicket>> GetResolvedTicketsAsync(CancellationToken cancellationToken);

    /// <summary>Closes the ticket if it is still Resolved; false if it has changed since.</summary>
    Task<bool> CloseResolvedTicketAsync(string ticketId, DateTimeOffset atUtc, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DigestTeam> Teams, IReadOnlyList<DigestTicket> Tickets)> GetDigestAsync(CancellationToken cancellationToken);
}

/// <summary>worker.NotificationLog: the record that makes every email at-most-once.</summary>
public interface INotificationLog
{
    /// <summary>True when this caller recorded the email first and should send it.</summary>
    Task<bool> TryClaimAsync(string ticketId, string eventKey, string recipient, DateTimeOffset sentAtUtc, CancellationToken cancellationToken);

    /// <summary>Removes a claim after a failed send, so the job's retry sends it.</summary>
    Task ReleaseAsync(string ticketId, string eventKey, string recipient, CancellationToken cancellationToken);
}

/// <summary>worker.JobCursor: when a polling job last completed.</summary>
public interface IJobCursorStore
{
    Task<DateTimeOffset?> GetAsync(string jobName, CancellationToken cancellationToken);

    Task SetAsync(string jobName, DateTimeOffset lastCompletedUtc, CancellationToken cancellationToken);
}
