namespace TopSupport.Models.Tickets;

/// <summary>
/// Backs GET /api/tickets/nav-counts: one round trip for the sidebar nav badge counts
/// (navItems()/workspaceNav() in topsupport-app.js), instead of five separate list fetches.
/// </summary>
public class TicketNavCountsDto
{
    /// <summary>End-user role: count of active (open/in-progress/awaiting-user) tickets
    /// where the acting user is requester or affected.</summary>
    public int MyOpenTickets { get; set; }

    /// <summary>Agent/admin role: the 5 built-in queue counts.</summary>
    public int QueueOpen { get; set; }
    public int QueueMine { get; set; }
    public int QueueOverdue { get; set; }
    public int QueueAnswered { get; set; }
    public int QueueClosed { get; set; }
}
