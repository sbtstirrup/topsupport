using System.Net;
using System.Text.RegularExpressions;

namespace TopSupport.Blazor.Services;

public static partial class HtmlText
{
    /// <summary>
    /// Port of textToHTML(s) in topsupport-app.js: HTML-encodes plain text, blank lines become
    /// paragraphs and single newlines &lt;br&gt;. Use it before plain text (a canned response with
    /// the ticket subject filled in, a close note) goes anywhere that is rendered as markup.
    /// </summary>
    public static string FromText(string? text)
        => string.Concat(BlankLines().Split(WebUtility.HtmlEncode(text ?? string.Empty).Replace("\r\n", "\n"))
            .Select(p => "<p>" + p.Replace("\n", "<br>") + "</p>"));

    [GeneratedRegex(@"\n{2,}")]
    private static partial Regex BlankLines();
}
