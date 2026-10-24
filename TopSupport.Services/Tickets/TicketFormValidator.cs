using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Exact port of validateTicketForm(F, agentMode) in topsupport-app.js (lines ~963-978):
/// agent-mode requires a chosen requester; topic is required; subject must be at least 5
/// characters once a topic is chosen; affected user is required; then every required field
/// in the topic's schema (whose ShowWhen, if any, currently evaluates true) must have a
/// non-empty value, with per-type error wording matching the demo exactly.
/// </summary>
public class TicketFormValidator : ITicketFormValidator
{
    public TicketFormValidationResult Validate(CreateTicketRequest request, bool agentMode)
    {
        var errors = new Dictionary<string, string>();

        if (agentMode && string.IsNullOrEmpty(request.RequesterUserId))
        {
            errors["requester"] = "Choose the person who is asking for help.";
        }

        if (string.IsNullOrEmpty(request.Topic))
        {
            errors["topic"] = "Choose a help topic.";
        }

        if (!string.IsNullOrEmpty(request.Topic))
        {
            var subject = (request.Subject ?? string.Empty).Trim();
            if (subject.Length == 0)
            {
                errors["subject"] = "Enter a short subject.";
            }
            else if (subject.Length < 5)
            {
                errors["subject"] = "Make the subject at least 5 characters so the team can find it.";
            }

            if (string.IsNullOrEmpty(request.AffectedUserId))
            {
                errors["affected"] = "Choose who is affected.";
            }

            foreach (var field in TicketTopicFieldSchemas.GetFields(request.Topic))
            {
                if (!field.Required) continue;
                if (field.ShowWhen != null && !field.ShowWhen.Evaluate(request.Fields)) continue;

                request.Fields.TryGetValue(field.Key, out var value);
                if (!HasValue(value))
                {
                    var label = field.Label.Replace(" (Optional)", string.Empty, StringComparison.OrdinalIgnoreCase);
                    var prefix = field.Type is TicketFieldType.Select or TicketFieldType.Radio
                        ? "Choose an answer for “"
                        : field.Type == TicketFieldType.Date
                            ? "Enter a date for “"
                            : "Fill in “";
                    errors[field.Key] = prefix + label + "”.";
                }
            }
        }

        return new TicketFormValidationResult { Errors = errors };
    }

    private static bool HasValue(object? value)
    {
        switch (value)
        {
            case null:
                return false;
            case string s:
                return s.Trim().Length > 0;
            case System.Collections.IEnumerable enumerable and not string:
                foreach (var _ in enumerable) return true;
                return false;
            default:
                return !string.IsNullOrWhiteSpace(value.ToString());
        }
    }
}
