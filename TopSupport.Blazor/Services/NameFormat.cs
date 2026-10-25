namespace TopSupport.Blazor.Services;

/// <summary>Avatar initials. (The "J. Moore" short name is TopSupport.Models.People.PersonName.Short.)</summary>
public static class NameFormat
{
    /// <summary>Avatar initials: "Nadia Hussain" -> "NH"; "?" for no name.</summary>
    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
