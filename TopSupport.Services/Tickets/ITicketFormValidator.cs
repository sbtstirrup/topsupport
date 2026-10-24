using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Server-side port of validateTicketForm(F, agentMode) from topsupport-app.js, run by
/// TicketService.CreateTicketAsync before any database write.
/// </summary>
public interface ITicketFormValidator
{
    TicketFormValidationResult Validate(CreateTicketRequest request, bool agentMode);
}
