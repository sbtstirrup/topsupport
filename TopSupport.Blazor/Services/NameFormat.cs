namespace TopSupport.Blazor.Services;

/// <summary>How the demo shortens people's names (avatars, timelines, tables).</summary>
public static class NameFormat
{
    /// <summary>Avatar initials: "Nadia Hussain" -> "NH"; "?" for no name.</summary>
    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }

    /// <summary>shortName() in the demo: "Nadia Hussain" -> "N. Hussain"; empty for no name.</summary>
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
