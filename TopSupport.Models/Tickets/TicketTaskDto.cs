namespace TopSupport.Models.Tickets;

/// <summary>
/// Mirrors a ticket's "tasks" checklist (e.g. the four-item leaver checklist seeded by
/// LEAVER_TASKS in topsupport-app.js).
/// </summary>
public class TicketTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string AssigneeAgentId { get; set; } = string.Empty;
    public DateOnly? Due { get; set; }
    public bool Done { get; set; }
}
