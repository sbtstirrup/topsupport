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

/// <summary>The values of <see cref="TicketActorRef.Type"/> (and of the timeline's ByType column).</summary>
public static class TicketActorTypes
{
    public const string User = "user";
    public const string Agent = "agent";
    public const string System = "system";
}
