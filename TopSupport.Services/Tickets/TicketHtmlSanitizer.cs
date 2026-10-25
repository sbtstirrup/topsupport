using Ganss.Xss;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Server-side port of cleanHTML(html) in topsupport-app.js, applied to every reply/note before it
/// is stored. The ticket pages render timeline HTML as raw markup, so this is the XSS boundary:
/// the Blazor rich-text editor sends whatever is in its contenteditable, and the Api can be called
/// directly. Allow-list rather than the demo's block-list: only the editor's formatting survives.
/// </summary>
public static class TicketHtmlSanitizer
{
    /// <summary>Reply/note HTML cap (the column is NVARCHAR(MAX)).</summary>
    public const int MaxLength = 100_000;

    private static readonly HtmlSanitizer Sanitizer = Create();

    public static string Sanitize(string? html) => Sanitizer.Sanitize(html ?? string.Empty);

    private static HtmlSanitizer Create()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = { "p", "br", "div", "span", "b", "strong", "i", "em", "u", "s", "strike", "ul", "ol", "li", "a", "blockquote", "code", "pre" },
            AllowedAttributes = { "href", "target", "rel" },
            AllowedSchemes = { "http", "https", "mailto" },
            UriAttributes = { "href" }
        };
        var sanitizer = new HtmlSanitizer(options);

        // As cleanHTML: links open in a new tab without access to the opener.
        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement a)
            {
                a.SetAttribute("target", "_blank");
                a.SetAttribute("rel", "noopener");
            }
        };
        return sanitizer;
    }
}
