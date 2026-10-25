namespace TopSupport.Models.Tickets;

/// <summary>
/// The attachment rules shared by the Api (enforced in TicketAttachmentService.AddAsync) and the
/// Blazor dropzone (the same checks and messages up front, for UX only -- the server is the
/// authority). The content type stored and served for a file is always derived from its
/// allow-listed extension, never taken from the client.
/// </summary>
public static class TicketAttachmentRules
{
    public const long MaxFileSizeBytes = 10L * 1024 * 1024;
    public const int MaxFilesPerTicket = 20;
    public const long MaxTotalBytesPerTicket = 50L * 1024 * 1024;
    public const int MaxFileNameLength = 200;

    public const string TypeNotAllowedMessage = "That file type isn't allowed.";
    public const string EmptyFileMessage = "That file is empty.";
    public const string TooLargeMessage = "That file is over 10 MB.";
    public static readonly string TooManyFilesMessage = $"A ticket can have at most {MaxFilesPerTicket} files.";
    public const string TotalTooLargeMessage = "A ticket's files can total at most 50 MB.";

    /// <summary>Allowed extension -> the content type stored and served for it.</summary>
    public static readonly IReadOnlyDictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".log"] = "text/plain",
        [".csv"] = "text/csv",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".zip"] = "application/zip",
        [".eml"] = "message/rfc822",
        [".msg"] = "application/vnd.ms-outlook"
    };

    /// <summary>Image types the download endpoint serves with their own type; everything else is application/octet-stream.</summary>
    public static readonly IReadOnlySet<string> ImageContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp"
    };

    /// <summary>The value for an &lt;input type="file" accept="..."&gt; matching the allow-list.</summary>
    public static readonly string AcceptAttribute = string.Join(",", ContentTypes.Keys);

    /// <summary>The content type for an allow-listed file name, or null when its extension isn't allowed.</summary>
    public static string? ContentTypeFor(string? fileName)
        => ContentTypes.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out var contentType) ? contentType : null;

    public static bool IsAllowed(string? fileName) => ContentTypeFor(fileName) is not null;

    /// <summary>
    /// The stored file name: no path, no control or Unicode format characters (bidi overrides such
    /// as U+202E, U+2066-U+2069, U+200E/U+200F, zero-width joiners), no characters that are invalid in
    /// a file name, whitespace collapsed to single spaces, and at most 200 characters with the
    /// extension kept. A name that is empty once cleaned becomes "file" + its extension.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));

        var cleaned = new System.Text.StringBuilder(name.Length);
        var pendingSpace = false;
        foreach (var c in name)
        {
            var category = char.GetUnicodeCategory(c);
            if (char.IsControl(c) || category == System.Globalization.UnicodeCategory.Format || "\"<>|:*?/".Contains(c)) continue;

            if (char.IsWhiteSpace(c) || category is System.Globalization.UnicodeCategory.SpaceSeparator
                    or System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator)
            {
                pendingSpace = cleaned.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                cleaned.Append(' ');
                pendingSpace = false;
            }

            cleaned.Append(c);
        }

        name = cleaned.ToString().Trim();
        var extension = Path.GetExtension(name);
        var stem = name[..^extension.Length].TrimEnd(' ', '.');

        var maxStem = MaxFileNameLength - extension.Length;
        if (maxStem < 1)
        {
            // An absurdly long "extension" can't be allow-listed anyway; keep the name bounded.
            return name[..Math.Min(name.Length, MaxFileNameLength)];
        }

        if (stem.Length > maxStem)
        {
            stem = stem[..maxStem];
            if (char.IsHighSurrogate(stem[^1])) stem = stem[..^1];
            stem = stem.TrimEnd(' ', '.');
        }

        return (stem.Length == 0 ? "file" : stem) + extension;
    }
}
