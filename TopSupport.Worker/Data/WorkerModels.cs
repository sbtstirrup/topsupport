using TopSupport.Models.Enums;

namespace TopSupport.Worker.Data;

/// <summary>The kinds of ticket event the ticket-events job emails about.</summary>
public enum TicketEventType
{
    Created,
    Reply,
    Assigned,
    Resolved
}

/// <summary>
/// One row of worker.usp_TicketEvents_GetSince. <see cref="EventId"/> is the ticket id for
/// Created and the timeline entry id otherwise; the Agent* fields are the ticket's CURRENT agent.
/// </summary>
public sealed record TicketEvent(
    TicketEventType Type,
    string EventId,
    DateTimeOffset At,
    string? EventText,
    string? ReplyHtml,
    string TicketId,
    string Number,
    string Subject,
    TicketStatus Status,
    string RequesterName,
    string RequesterEmail,
    string? AgentId,
    string? AgentName,
    string? AgentEmail);

/// <summary>An active ticket with no agent reply yet, for the SLA sweep (worker.usp_SlaSweep_GetCandidates).</summary>
public sealed record SlaCandidate(
    string TicketId,
    string Number,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    DateTimeOffset Created,
    DateTimeOffset? FirstAgentReplyAt,
    string? AgentName,
    string? AgentEmail,
    string? TeamId,
    string? TeamName);

/// <summary>An active agent on a team.</summary>
public sealed record TeamMember(string TeamId, string AgentId, string Name, string Email);

/// <summary>A Resolved ticket and when it was resolved (worker.usp_AutoClose_GetResolved).</summary>
public sealed record ResolvedTicket(string TicketId, string Number, DateTimeOffset ResolvedAt);

/// <summary>A team with an active lead, for the daily digest.</summary>
public sealed record DigestTeam(string TeamId, string TeamName, string LeadName, string LeadEmail);

/// <summary>An active ticket assigned to a team, for the daily digest.</summary>
public sealed record DigestTicket(
    string TicketId,
    string Number,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    DateTimeOffset Created,
    string TeamId,
    DateTimeOffset? FirstAgentReplyAt,
    string? AgentName);
