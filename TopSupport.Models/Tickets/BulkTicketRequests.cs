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

/// <summary>
/// Bulk close, mirroring MODALS.close / ACT['close-go']: closes every ticket with Status
/// (Resolved -- the modal's default -- or Closed), first adding the optional internal Note to each.
/// Tickets already Closed (final), or already at the chosen status, are skipped.
/// </summary>
public class BulkCloseRequest
{
    /// <summary>Most tickets a single bulk action may touch (also the cap for status/priority/assign).</summary>
    public const int MaxTickets = 100;

    /// <summary>Longest internal note (plain text) the Close modal accepts.</summary>
    public const int MaxNoteLength = 2000;

    public List<string> TicketIds { get; set; } = new();

    /// <summary>"Resolved" | "Closed". Defaults to Resolved.</summary>
    public string Status { get; set; } = "Resolved";

    /// <summary>Optional plain-text internal note added to each ticket before it is closed.</summary>
    public string? Note { get; set; }
}
