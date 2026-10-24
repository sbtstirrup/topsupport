namespace TopSupport.Models.Enums;

/// <summary>
/// Mirrors the timeline entry "kind" discriminator used by addEntry/addEvent in
/// topsupport-app.js: system/user events, agent-only internal notes, and public replies.
/// </summary>
public enum TicketTimelineEntryKind
{
    Event,
    Note,
    Reply
}
