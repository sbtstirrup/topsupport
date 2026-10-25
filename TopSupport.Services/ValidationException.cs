namespace TopSupport.Services;

/// <summary>
/// The user's input breaks a rule: field key -> message to show beside that field (the demo's
/// F.errs). The one way services report invalid input; the Api turns it into a 400
/// ValidationProblemDetails. Bugs keep using ArgumentException and friends, which become a 500.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string> errors)
        : base("Validation failed: " + string.Join("; ", errors.Select(e => $"{e.Key}: {e.Value}")))
    {
        Errors = errors;
    }

    /// <summary>A single rule; <paramref name="field"/> is empty for one that isn't about a field.</summary>
    public ValidationException(string field, string message)
        : this(new Dictionary<string, string> { [field] = message })
    {
    }

    public IReadOnlyDictionary<string, string> Errors { get; }
}
