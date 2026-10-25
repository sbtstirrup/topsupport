namespace TopSupport.Models.Tickets;

/// <summary>
/// Input to TicketService.CreateTicketAsync, mirroring the `data` object passed to
/// createTicket(data) in topsupport-app.js: topic + subject + who's asking/affected + the
/// topic's dynamic field answers. CreatedBy (who is actually submitting the form — the
/// requester themselves, or an agent raising on their behalf) is passed separately to the
/// service method rather than carried on this request.
/// </summary>
public class CreateTicketRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string RequesterUserId { get; set; } = string.Empty;
    public string AffectedUserId { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
}
