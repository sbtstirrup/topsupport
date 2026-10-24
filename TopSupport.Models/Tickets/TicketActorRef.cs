namespace TopSupport.Models.Tickets;

/// <summary>
/// Mirrors the small { t, id } "actor reference" shape used throughout topsupport-app.js
/// (createdBy, timeline entry "by", etc.) to say who did something: a user, an agent, or
/// the system itself.
/// </summary>
public class TicketActorRef
{
    /// <summary>"user" | "agent" | "system".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Null when Type is "system".</summary>
    public string? Id { get; set; }
}
