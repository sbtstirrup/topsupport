namespace TopSupport.Models.People;

/// <summary>
/// shortName() in the demo, used wherever a person is named in a timeline, table or email:
/// "Jamie Moore" -> "J. Moore". One copy on purpose: the worker matches the Api's
/// "Assigned to J. Moore" timeline text, so the two must never drift apart.
/// </summary>
public static class PersonName
{
    /// <summary>"Jamie Moore" -> "J. Moore"; a single name stays as it is; empty for no name.</summary>
    public static string Short(string? name)
    {
        var parts = (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => $"{parts[0][0]}. {parts[^1]}"
        };
    }
}
