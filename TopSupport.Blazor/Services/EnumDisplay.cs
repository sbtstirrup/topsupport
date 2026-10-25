using TopSupport.Models.Enums;

namespace TopSupport.Blazor.Services;

/// <summary>
/// Maps PascalCase enum members back to the demo's human-readable display strings (e.g.
/// TicketStatus.InProgress -> "In Progress", matching the STATUSES/PRIORITIES constants in
/// topsupport-app.js). Used by StatusBadge/PriorityBadge and anywhere else a ticket enum is
/// shown to a user.
/// </summary>
public static class EnumDisplay
{
    public static string Label(TicketStatus status) => TicketStatusText.Label(status);

    public static string Label(TicketPriority priority) => priority switch
    {
        TicketPriority.Urgent => "Urgent",
        TicketPriority.High => "High",
        TicketPriority.Normal => "Normal",
        TicketPriority.Low => "Low",
        _ => priority.ToString()
    };

    /// <summary>CSS class suffix for status badges (st-open/st-progress/st-awaiting/st-resolved/st-closed).</summary>
    public static string CssClass(TicketStatus status) => status switch
    {
        TicketStatus.Open => "st-open",
        TicketStatus.InProgress => "st-progress",
        TicketStatus.AwaitingUser => "st-awaiting",
        TicketStatus.Resolved => "st-resolved",
        TicketStatus.Closed => "st-closed",
        _ => "st-open"
    };

    /// <summary>CSS class suffix for priority badges (pr-urgent/pr-high/pr-normal/pr-low).</summary>
    public static string CssClass(TicketPriority priority) => priority switch
    {
        TicketPriority.Urgent => "pr-urgent",
        TicketPriority.High => "pr-high",
        TicketPriority.Normal => "pr-normal",
        TicketPriority.Low => "pr-low",
        _ => "pr-normal"
    };

    /// <summary>Icon key for a status badge (ST_CLASS[s][1] in the demo).</summary>
    public static string Icon(TicketStatus status) => status switch
    {
        TicketStatus.Open => "inbox",
        TicketStatus.InProgress => "refresh",
        TicketStatus.AwaitingUser => "clock",
        TicketStatus.Resolved => "check",
        TicketStatus.Closed => "lock",
        _ => "inbox"
    };

    /// <summary>Icon key for a priority badge (PR_CLASS[p][1] in the demo); null means no icon (Normal).</summary>
    public static string? Icon(TicketPriority priority) => priority switch
    {
        TicketPriority.Urgent => "alert",
        TicketPriority.High => "up",
        TicketPriority.Normal => null,
        TicketPriority.Low => "down",
        _ => null
    };
}
