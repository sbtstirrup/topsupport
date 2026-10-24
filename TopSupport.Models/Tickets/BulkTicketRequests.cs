namespace TopSupport.Models.Tickets;

/// <summary>
/// Bulk-action request DTOs, mirroring the queue page's bulk-action bar which loops the
/// single-ticket helpers over a selection (`ts.forEach(...)` in topsupport-app.js).
/// </summary>
public class BulkStatusRequest
{
    public List<string> TicketIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public class BulkPriorityRequest
{
    public List<string> TicketIds { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
}

public class BulkAssignRequest
{
    public List<string> TicketIds { get; set; } = new();

    /// <summary>"agent" | "team".</summary>
    public string Kind { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
}

/// <summary>Bulk close: sets Status to Resolved (the demo's Close modal default) for every ticket id.</summary>
public class BulkCloseRequest
{
    public List<string> TicketIds { get; set; } = new();
}
