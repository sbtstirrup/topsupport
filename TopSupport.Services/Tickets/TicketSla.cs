using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Static SLA/overdue/answered helpers, porting SLA_HOURS/slaDue/isOverdue/isAnswered/
/// firstResponse/ACTIVE from topsupport-app.js exactly. Kept in C#, not T-SQL, per the plan:
/// QueueService applies this over rows returned by usp_Ticket_GetActiveForQueue/
/// usp_Ticket_SearchForQueue rather than duplicating the logic in a stored procedure.
/// </summary>
public static class TicketSla
{
    /// <summary>Response-time targets in hours, keyed by priority (SLA_HOURS in the demo).</summary>
    public static readonly IReadOnlyDictionary<TicketPriority, int> SlaHours = new Dictionary<TicketPriority, int>
    {
        [TicketPriority.Urgent] = 4,
        [TicketPriority.High] = 8,
        [TicketPriority.Normal] = 24,
        [TicketPriority.Low] = 72
    };

    /// <summary>Statuses considered "active" (ACTIVE in the demo) — everything but Resolved/Closed.</summary>
    public static readonly IReadOnlyList<TicketStatus> ActiveStatuses = new[]
    {
        TicketStatus.Open, TicketStatus.InProgress, TicketStatus.AwaitingUser
    };

    public static bool IsActive(TicketDto ticket) => ActiveStatuses.Contains(ticket.Status);

    public static DateTimeOffset SlaDue(TicketDto ticket) => ticket.Created.AddHours(SlaHours[ticket.Priority]);

    /// <summary>True once an agent has posted at least one public reply (slaDue is a deadline
    /// for the *first* agent response, matching firstResponse(t) in the demo).</summary>
    public static bool HasFirstResponse(TicketDto ticket) => ticket.FirstAgentReplyAt.HasValue;

    /// <summary>Active, no agent reply yet, and past its response target at <paramref name="now"/>.</summary>
    public static bool IsOverdue(TicketDto ticket, DateTimeOffset now) =>
        IsActive(ticket) && !HasFirstResponse(ticket) && now > SlaDue(ticket);

    /// <summary>True when the ticket is active and the most recent public reply came from an agent.</summary>
    public static bool IsAnswered(TicketDto ticket) =>
        IsActive(ticket) && ticket.LastReplyByType == "agent";
}
