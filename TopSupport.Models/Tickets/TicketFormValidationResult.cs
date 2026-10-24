namespace TopSupport.Models.Tickets;

/// <summary>
/// Result of ITicketFormValidator.Validate, mirroring the `e` error-map returned by
/// validateTicketForm(F, agentMode) in topsupport-app.js: a field-key -> message dictionary,
/// empty when the form is valid.
/// </summary>
public class TicketFormValidationResult
{
    public Dictionary<string, string> Errors { get; set; } = new();

    public bool IsValid => Errors.Count == 0;
}
