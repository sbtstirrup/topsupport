using System.Globalization;
using System.Text.Json;
using TopSupport.Models.Admin;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Port of validateTicketForm(F, agentMode) in topsupport-app.js (lines ~963-978), with the
/// demo's wording: requester is required; topic is required; subject must be 5-300 characters
/// once a topic is chosen; affected user is required; then every required field in the topic's
/// schema (whose ShowWhen, if any, currently evaluates true) must have a non-empty value.
///
/// On top of the demo, the server is authoritative over what gets stored: requester/affected
/// must be real, non-deleted users; every answer key must be a field of the topic; Select/Radio/
/// Checks answers must be among the options resolved for the affected user; dates must be real
/// yyyy-MM-dd dates in 2000-2100; text answers are length-capped and must be JSON strings; and
/// the serialized answers are capped at FieldLimits.TopicFieldsJson.
/// </summary>
public class TicketFormValidator : ITicketFormValidator
{
    private static readonly DateOnly MinDate = new(2000, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);

    // Measures answers by their characters, not the default encoder's \uXXXX escapes, so
    // non-ASCII text isn't penalised six-fold against the size cap.
    private static readonly JsonSerializerOptions MeasureJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IUserRepository _users;
    private readonly ITicketFieldOptionsResolver _optionsResolver;

    public TicketFormValidator(IUserRepository users, ITicketFieldOptionsResolver optionsResolver)
    {
        _users = users;
        _optionsResolver = optionsResolver;
    }

    public async Task<TicketFormValidationResult> ValidateAsync(CreateTicketRequest request, bool agentMode, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>();
        var fields = request.Fields ?? new Dictionary<string, object?>();

        // The demo only asks in agent mode (end users are always their own requester), but an
        // empty or unknown id must never reach the insert as an FK failure.
        if (string.IsNullOrEmpty(request.RequesterUserId))
        {
            errors["requester"] = "Choose the person who is asking for help.";
        }
        else if (!await IsLiveUserAsync(request.RequesterUserId, cancellationToken))
        {
            errors["requester"] = "We couldn't find that person. Choose someone from the list.";
        }

        if (string.IsNullOrEmpty(request.Topic) || TicketTopics.Find(request.Topic) is null)
        {
            errors["topic"] = "Choose a help topic.";
            return new TicketFormValidationResult { Errors = errors };
        }

        var subject = (request.Subject ?? string.Empty).Trim();
        if (subject.Length == 0)
        {
            errors["subject"] = "Enter a short subject.";
        }
        else if (subject.Length < 5)
        {
            errors["subject"] = "Make the subject at least 5 characters so the team can find it.";
        }
        else if (subject.Length > FieldLimits.TicketSubject)
        {
            errors["subject"] = $"Keep the subject to {FieldLimits.TicketSubject} characters or fewer.";
        }

        if (string.IsNullOrEmpty(request.AffectedUserId))
        {
            errors["affected"] = "Choose who is affected.";
        }
        else if (!await IsLiveUserAsync(request.AffectedUserId, cancellationToken))
        {
            errors["affected"] = "We couldn't find that person. Choose someone from the list.";
        }

        if (JsonSerializer.Serialize(fields, MeasureJson).Length > FieldLimits.TopicFieldsJson)
        {
            errors["fields"] = "Your answers are too long. Shorten them and try again.";
            return new TicketFormValidationResult { Errors = errors };
        }

        // Asset and person option lists are the affected user's, exactly as the form showed them.
        var affectedForOptions = errors.ContainsKey("affected") ? null : request.AffectedUserId;
        var schema = await _optionsResolver.ResolveAsync(request.Topic, affectedForOptions, fields, cancellationToken);

        var knownKeys = schema.Where(f => f.Type != TicketFieldType.Banner).Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        if (fields.Keys.Any(k => !knownKeys.Contains(k)))
        {
            errors["fields"] = "This form has an answer we don't recognise. Reload the page and try again.";
        }

        foreach (var field in VisibleFields(schema, fields))
        {
            fields.TryGetValue(field.Key, out var raw);
            var value = ToJson(raw);
            var label = field.Label.Replace(" (Optional)", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (IsBlank(value))
            {
                if (field.Required)
                {
                    var prefix = field.Type is TicketFieldType.Select or TicketFieldType.Radio
                        ? "Choose an answer for “"
                        : field.Type == TicketFieldType.Date
                            ? "Enter a date for “"
                            : "Fill in “";
                    errors[field.Key] = prefix + label + "”.";
                }
                continue;
            }

            if (CheckAnswer(field, value, label) is { } problem)
            {
                errors[field.Key] = problem;
            }
        }

        return new TicketFormValidationResult { Errors = errors };
    }

    /// <summary>
    /// The topic's answerable fields that are currently shown, in schema order. ShowWhen is
    /// evaluated only against answers to fields that are themselves shown, so a stale answer
    /// left behind a hidden field (e.g. hardware's device after deviceType is cleared) can't
    /// reveal its dependants. TicketService stores only these fields' answers.
    /// </summary>
    public static List<TicketFieldDescriptor> VisibleFields(IEnumerable<TicketFieldDescriptor> schema, IReadOnlyDictionary<string, object?> fields)
    {
        var shownValues = new Dictionary<string, object?>();
        var visible = new List<TicketFieldDescriptor>();
        foreach (var field in schema)
        {
            if (field.Type == TicketFieldType.Banner) continue;
            if (field.ShowWhen != null && !field.ShowWhen.Evaluate(shownValues)) continue;

            visible.Add(field);
            if (fields.TryGetValue(field.Key, out var value)) shownValues[field.Key] = value;
        }
        return visible;
    }

    /// <summary>A Date answer as posted (string or JsonElement string): exact yyyy-MM-dd, 2000-01-01 to 2100-12-31.</summary>
    public static bool TryParseFieldDate(object? value, out DateOnly date)
    {
        var json = ToJson(value);
        date = default;
        return json.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(json.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            && date >= MinDate && date <= MaxDate;
    }

    private static string? CheckAnswer(TicketFieldDescriptor field, JsonElement value, string label)
    {
        var wrongKind = $"Check your answer for “{label}”.";
        var options = field.StaticOptions?.Select(o => o.Value).ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>();

        switch (field.Type)
        {
            case TicketFieldType.Text:
            case TicketFieldType.TextArea:
            {
                if (value.ValueKind != JsonValueKind.String) return wrongKind;
                var max = field.Type == TicketFieldType.Text ? FieldLimits.TopicText : FieldLimits.TopicTextArea;
                return value.GetString()!.Trim().Length > max ? $"Shorten “{label}” to {max:N0} characters or fewer." : null;
            }

            case TicketFieldType.Select:
            case TicketFieldType.Radio:
                if (value.ValueKind != JsonValueKind.String) return wrongKind;
                return options.Contains(value.GetString()!) ? null : $"Choose one of the listed answers for “{label}”.";

            case TicketFieldType.Checks:
            {
                if (value.ValueKind != JsonValueKind.Array) return wrongKind;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || !options.Contains(item.GetString()!) || !seen.Add(item.GetString()!))
                    {
                        return $"Choose from the listed answers for “{label}”.";
                    }
                }
                return null;
            }

            case TicketFieldType.Date:
                if (value.ValueKind != JsonValueKind.String) return wrongKind;
                if (!DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return $"Enter a real date for “{label}”.";
                }
                return date < MinDate || date > MaxDate ? $"Enter a date between 2000 and 2100 for “{label}”." : null;

            case TicketFieldType.Files:
                // File metadata only (the bytes go through the attachments endpoint); the
                // TopicFieldsJson cap above bounds its size.
                return value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(i => i.ValueKind == JsonValueKind.Object)
                    ? null
                    : wrongKind;

            default:
                return wrongKind;
        }
    }

    private async Task<bool> IsLiveUserAsync(string userId, CancellationToken cancellationToken)
        => await _users.GetByIdAsync(userId, cancellationToken) is { Deleted: false };

    /// <summary>Answers posted to the Api are JsonElements; in-process callers may pass strings/lists.</summary>
    private static JsonElement ToJson(object? value) => value switch
    {
        null => default,
        JsonElement json => json,
        _ => JsonSerializer.SerializeToElement(value)
    };

    /// <summary>Unanswered: missing, null, whitespace or an empty list. Anything else ({}, false, 0)
    /// counts as an answer for the required check and is then rejected by CheckAnswer.</summary>
    private static bool IsBlank(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Array => value.GetArrayLength() == 0,
        _ => false
    };
}
