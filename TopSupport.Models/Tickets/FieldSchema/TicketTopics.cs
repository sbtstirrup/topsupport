namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>One entry in the TOPICS catalog (topsupport-app.js's const TOPICS).</summary>
public class TicketTopic
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>"" | "Issues" | "Requests" — groups topics in the raise-ticket topic picker.</summary>
    public string Group { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Static port of the TOPICS/DEPTS constants in topsupport-app.js: the 7 help-desk topics and
/// the 2 departments that own them.
/// </summary>
public static class TicketTopics
{
    public static readonly IReadOnlyList<string> DEPTS = new[] { "IT Business Support", "Infrastructure" };

    public static readonly IReadOnlyList<TicketTopic> TOPICS = new[]
    {
        new TicketTopic { Id = "general", Label = "General Enquiry", Group = "", Department = "IT Business Support" },
        new TicketTopic { Id = "hardware", Label = "Hardware", Group = "Issues", Department = "IT Business Support" },
        new TicketTopic { Id = "software", Label = "Software/Application", Group = "Issues", Department = "IT Business Support" },
        new TicketTopic { Id = "leaver", Label = "Leaver", Group = "Requests", Department = "IT Business Support" },
        new TicketTopic { Id = "newuser", Label = "New User PC Setup", Group = "Requests", Department = "IT Business Support" },
        new TicketTopic { Id = "server", Label = "Server", Group = "Requests", Department = "Infrastructure" },
        new TicketTopic { Id = "vpn", Label = "VPN Setup", Group = "Requests", Department = "Infrastructure" }
    };

    public static TicketTopic? Find(string id) => TOPICS.FirstOrDefault(t => t.Id == id);
}
