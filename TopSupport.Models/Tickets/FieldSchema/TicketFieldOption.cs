namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// One choice in a Select/Radio/Checks field, mirroring the demo's option shape (either a
/// plain string or a { v, l, d } object, e.g. IMPACT/NEW_DEVICES in topsupport-app.js).
/// </summary>
public class TicketFieldOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
}
