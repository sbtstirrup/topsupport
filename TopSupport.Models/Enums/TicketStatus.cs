namespace TopSupport.Models.Enums;

/// <summary>
/// Mirrors the STATUSES constant in topsupport-app.js.
/// </summary>
public enum TicketStatus
{
    Open,
    InProgress,
    AwaitingUser,
    Resolved,
    Closed
}

public static class TicketStatusText
{
    /// <summary>The demo's STATUSES wording ("In Progress", not "InProgress").</summary>
    public static string Label(TicketStatus status) => status switch
    {
        TicketStatus.InProgress => "In Progress",
        TicketStatus.AwaitingUser => "Awaiting User",
        _ => status.ToString()
    };
}
