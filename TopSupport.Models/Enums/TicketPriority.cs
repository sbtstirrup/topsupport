namespace TopSupport.Models.Enums;

/// <summary>
/// Mirrors the PRIORITIES constant in topsupport-app.js. Order matters for display
/// (most to least urgent) and matches SLA_HOURS keys in the source demo.
/// </summary>
public enum TicketPriority
{
    Urgent,
    High,
    Normal,
    Low
}
