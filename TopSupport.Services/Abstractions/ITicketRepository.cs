using TopSupport.Models.Tickets;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract implemented by TopSupport.Api's ADO.NET repository layer
/// (TopSupport.Api/Data/TicketRepository.cs), calling the stored procedures defined in
/// TopSupport.Database/Scripts/03_StoredProcedures_Core.sql and
/// 05_StoredProcedures_Extensions.sql. Kept in Services (not Models) so it can depend on
/// nothing but the shared DTOs, while living next to the service that consumes it.
/// </summary>
public interface ITicketRepository
{
    Task<TicketDto?> GetByIdAsync(string ticketId, CancellationToken cancellationToken = default);

    /// <summary>"My Tickets": every ticket where the user is requester OR affected (usp_Ticket_GetByRequesterOrAffected).</summary>
    Task<IReadOnlyList<TicketDto>> GetByRequesterOrAffectedAsync(string userId, CancellationToken cancellationToken = default);

    Task<TicketDto> CreateAsync(TicketDto ticket, CancellationToken cancellationToken = default);

    /// <summary>SELECT NEXT VALUE FOR dbo.TicketNumberSeq (usp_Ticket_NextNumber) — an atomic,
    /// race-free replacement for the demo's in-memory DB.nextNum counter.</summary>
    Task<int> GetNextNumberAsync(CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string ticketId, string status, DateTimeOffset updated, CancellationToken cancellationToken = default);

    Task UpdatePriorityAsync(string ticketId, string priority, DateTimeOffset updated, CancellationToken cancellationToken = default);

    Task UpdateTopicAsync(string ticketId, string topic, string department, DateTimeOffset updated, CancellationToken cancellationToken = default);

    /// <summary>Writes whichever nullable (agentId, teamId) pair the caller computed — see
    /// usp_Ticket_Assign's doc comment: the service layer pre-computes the correct pair.</summary>
    Task AssignAsync(string ticketId, string? assignedAgentId, string? assignedTeamId, DateTimeOffset updated, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claim (usp_Ticket_ClaimIfUnchanged): assigns the agent/team pair only while the ticket's
    /// agent is still <paramref name="expectedAgentId"/> (null = unassigned). False when another
    /// request changed the assignee first, so two simultaneous claims can't both succeed.
    /// </summary>
    Task<bool> ClaimIfUnchangedAsync(string ticketId, string agentId, string? assignedTeamId, string? expectedAgentId, DateTimeOffset updated, CancellationToken cancellationToken = default);

    /// <summary>Inserts a timeline entry (usp_TicketTimelineEntry_Insert, which also bumps
    /// Tickets.Updated) and returns the entry as persisted.</summary>
    Task<TicketTimelineEntryDto> AppendTimelineEntryAsync(TicketTimelineEntryDto entry, CancellationToken cancellationToken = default);

    Task<TicketTaskDto> InsertTaskAsync(TicketTaskDto task, CancellationToken cancellationToken = default);

    Task SetTaskDoneAsync(string taskId, bool done, CancellationToken cancellationToken = default);

    /// <summary>All tickets with Status in (Open, InProgress, AwaitingUser), each with
    /// FirstAgentReplyAt/LastReplyByType populated for QueueService's SLA/answered logic.</summary>
    Task<IReadOnlyList<TicketDto>> GetActiveForQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>Equality-filtered + optional free-text search (usp_Ticket_SearchForQueue);
    /// null parameters mean "no filter on that column". No sorting/paging — QueueService does
    /// that in C# over the returned rows.</summary>
    Task<IReadOnlyList<TicketDto>> SearchForQueueAsync(
        string? status,
        string? priority,
        string? topic,
        string? department,
        string? assignedAgentId,
        string? assignedTeamId,
        string? searchText,
        CancellationToken cancellationToken = default);
}
