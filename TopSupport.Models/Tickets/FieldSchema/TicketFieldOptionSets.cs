namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// Static option lists shared by multiple topics, ported verbatim from the corresponding
/// const declarations near the top of topsupport-app.js (IMPACT, DEVICE_TYPES, PROBLEMS,
/// STARTED, TRIED, APPS, SOFTWARE, NEW_DEVICES, DEPARTMENTS_ORG).
/// </summary>
public static class TicketFieldOptionSets
{
    /// <summary>How much an issue affects the user's work (topicFields' shared "impact" field).</summary>
    public static readonly IReadOnlyList<TicketFieldOption> IMPACT = new[]
    {
        new TicketFieldOption { Value = "cant", Label = "I can't work at all", Description = "Nothing I can do lets me carry on with my work." },
        new TicketFieldOption { Value = "difficulty", Label = "I can work, but with difficulty", Description = "I have a workaround, but it is slowing me down." },
        new TicketFieldOption { Value = "inconvenience", Label = "It's an inconvenience", Description = "It is annoying, but my work is not held up." }
    };

    /// <summary>Maps an IMPACT value to the ticket priority it implies (IMPACT_PRI in the demo).</summary>
    public static readonly IReadOnlyDictionary<string, string> IMPACT_PRI = new Dictionary<string, string>
    {
        ["cant"] = "Urgent",
        ["difficulty"] = "High",
        ["inconvenience"] = "Normal"
    };

    public static readonly IReadOnlyList<string> DEVICE_TYPES = new[]
    {
        "Laptop", "Desktop", "Monitor", "Docking station", "Headset/Phone", "Printer", "Keyboard/Mouse", "Other"
    };

    /// <summary>Maps a DEVICE_TYPES value to the Assets.Type it corresponds to, for filtering
    /// the affected user's assets down to the relevant device type (DEVICE_ASSET in the demo).</summary>
    public static readonly IReadOnlyDictionary<string, string> DEVICE_ASSET = new Dictionary<string, string>
    {
        ["Laptop"] = "laptop",
        ["Desktop"] = "desktop",
        ["Monitor"] = "monitor",
        ["Docking station"] = "dock",
        ["Headset/Phone"] = "headset"
    };

    public static readonly IReadOnlyList<string> PROBLEMS = new[]
    {
        "Won't turn on", "Restarting or crashing", "Running slowly", "Display problem",
        "Audio or call problem", "Network or Wi-Fi problem", "Physical damage",
        "Accessory not working", "Other"
    };

    public static readonly IReadOnlyList<string> STARTED = new[] { "Today", "Yesterday", "This week", "Longer than a week" };

    public static readonly IReadOnlyList<string> TRIED = new[]
    {
        "Restarted the device", "Checked cables and power", "Tried a different port or dock",
        "Tried another device", "Nothing yet"
    };

    public static readonly IReadOnlyList<string> APPS = new[]
    {
        "Microsoft Outlook", "Microsoft Teams", "Microsoft Excel", "Microsoft Word", "Adobe Acrobat",
        "Salesforce", "Sage 200", "Google Chrome", "OneDrive", "SharePoint"
    };

    public static readonly IReadOnlyList<string> SOFTWARE = new[]
    {
        "Microsoft 365", "Teams Phone", "Salesforce", "Sage 200", "Adobe Acrobat Pro", "VPN client", "Power BI"
    };

    public static readonly IReadOnlyList<TicketFieldOption> NEW_DEVICES = new[]
    {
        new TicketFieldOption { Value = "Standard laptop", Label = "Standard laptop", Description = "Dell Latitude 5440 with dock and 24-inch monitor." },
        new TicketFieldOption { Value = "Power laptop", Label = "Power laptop", Description = "Dell Latitude 7440 for heavy data or design work." },
        new TicketFieldOption { Value = "Desktop", Label = "Desktop", Description = "Dell OptiPlex for fixed desks such as the warehouse." },
        new TicketFieldOption { Value = "Use existing equipment", Label = "Use existing equipment", Description = "Re-use a device already at the desk." }
    };

    public static readonly IReadOnlyList<string> DEPARTMENTS_ORG = new[]
    {
        "Customer Services", "Finance", "HR", "Legal", "Marketing", "Operations", "Product", "Sales"
    };

    public static List<TicketFieldOption> FromStrings(IEnumerable<string> values) =>
        values.Select(v => new TicketFieldOption { Value = v, Label = v }).ToList();
}
