using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Server-side port of validateTicketForm(F, agentMode) from topsupport-app.js, run by
/// TicketService.CreateTicketAsync before any database write. Async because the server is
/// authoritative: requester/affected users must exist, and select/check answers must be among
/// the options resolved for the ticket's affected user.
/// </summary>
public interface ITicketFormValidator
{
    Task<TicketFormValidationResult> ValidateAsync(CreateTicketRequest request, bool agentMode, CancellationToken cancellationToken = default);
}
