namespace TopSupport.Models.Tickets;

/// <summary>
/// Input to TicketService.AssignToAgentAsync/AssignToTeamAsync, mirroring the (kind, id)
/// pair assignTicket(t, kind, id) takes in topsupport-app.js.
/// </summary>
public class AssignTicketRequest
{
    /// <summary>"agent" | "team".</summary>
    public string Kind { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
}
