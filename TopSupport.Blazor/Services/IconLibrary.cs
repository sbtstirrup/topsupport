namespace TopSupport.Blazor.Services;

/// <summary>
/// Direct port of the ICONS map and ic() helper from topsupport-app.js: a small inline-SVG
/// icon set (Lucide-style strokes) used throughout the sidebar nav and badges. Only the
/// icons this skeleton's nav/shell actually reference are included; add more here as
/// screens are fleshed out, copying the path data straight from the source ICONS map.
/// </summary>
public static class IconLibrary
{
    private static readonly Dictionary<string, string> Icons = new()
    {
        ["ticket"] = "<path d=\"M3 7a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 0 0 4v3a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-3a2 2 0 0 0 0-4z\"/><path d=\"M14 5v14\" stroke-dasharray=\"2 2\"/>",
        ["inbox"] = "<path d=\"M22 12h-6l-2 3h-4l-2-3H2\"/><path d=\"M5.5 5h13l3.5 7v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-6z\"/>",
        ["user"] = "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21a8 8 0 0 1 16 0\"/>",
        ["users"] = "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2 20a7 7 0 0 1 14 0\"/><path d=\"M16 4.5a3.5 3.5 0 0 1 0 7M18 13.5a6 6 0 0 1 4 6.5\"/>",
        ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
        ["reply"] = "<path d=\"M9 14 4 9l5-5\"/><path d=\"M4 9h10a6 6 0 0 1 6 6v4\"/>",
        ["lock"] = "<rect x=\"4\" y=\"11\" width=\"16\" height=\"10\" rx=\"2\"/><path d=\"M8 11V7a4 4 0 0 1 8 0v4\"/>",
        ["note"] = "<path d=\"M4 4h12l4 4v12H4z\"/><path d=\"M8 10h8M8 14h6\"/>",
        ["chart"] = "<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/>",
        ["building"] = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"1\"/><path d=\"M9 7h1M14 7h1M9 11h1M14 11h1M9 15h1M14 15h1M10 21v-3h4v3\"/>",
        ["team"] = "<circle cx=\"12\" cy=\"7\" r=\"3\"/><circle cx=\"5\" cy=\"10\" r=\"2.5\"/><circle cx=\"19\" cy=\"10\" r=\"2.5\"/><path d=\"M7 20a5 5 0 0 1 10 0M1 19a4 4 0 0 1 5-3.8M23 19a4 4 0 0 0-5-3.8\"/>",
        ["key"] = "<circle cx=\"8\" cy=\"15\" r=\"4\"/><path d=\"m11 12 9-9M17 6l3 3\"/>",
        ["shield"] = "<path d=\"M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z\"/>",
        ["book"] = "<path d=\"M4 4h6a3 3 0 0 1 3 3v13a2 2 0 0 0-2-2H4z\"/><path d=\"M20 4h-6a3 3 0 0 0-3 3v13a2 2 0 0 1 2-2h7z\"/>",
        ["page"] = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><path d=\"M8 8h8M8 12h8M8 16h5\"/>",
        ["list"] = "<path d=\"M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01\"/>",
        ["filter"] = "<path d=\"M3 5h18l-7 8v6l-4 2v-8z\"/>",
        ["refresh"] = "<path d=\"M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5\"/>",
        ["logout"] = "<path d=\"M15 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4M10 8l-4 4 4 4M6 12h11\"/>",
        ["menu"] = "<path d=\"M4 6h16M4 12h16M4 18h16\"/>",
        ["check"] = "<path d=\"M20 6 9 17l-5-5\"/>",

        // Tier 3 additions -- copied verbatim from the ICONS const in topsupport-app.js (~line 356).
        ["send"] = "<path d=\"m22 2-7 20-4-9-9-4z\"/><path d=\"M22 2 11 13\"/>",
        ["edit"] = "<path d=\"M4 20h4L19 9l-4-4L4 16z\"/><path d=\"m14 6 4 4\"/>",
        ["trash"] = "<path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/>",
        ["flag"] = "<path d=\"M5 21V4M5 4h11l-2 4 2 4H5\"/>",
        ["alert"] = "<path d=\"M12 3 2 20h20z\"/><path d=\"M12 10v4M12 17h.01\"/>",
        ["clip"] = "<path d=\"m21 11-8.5 8.5a5 5 0 0 1-7-7L14 4a3.5 3.5 0 0 1 5 5l-8.5 8.5a2 2 0 0 1-3-3L15 7\"/>",
        ["save"] = "<path d=\"M5 3h11l5 5v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z\"/><path d=\"M7 3v5h8M7 21v-7h10v7\"/>",
        ["back"] = "<path d=\"m15 6-6 6 6 6\"/>",
        ["eye"] = "<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        ["laptop"] = "<rect x=\"4\" y=\"5\" width=\"16\" height=\"11\" rx=\"1.5\"/><path d=\"M2 19h20\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20 20-4-4\"/>",
        ["x"] = "<path d=\"M18 6 6 18M6 6l12 12\"/>",
        ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 11v5M12 8h.01\"/>",
        ["phone"] = "<path d=\"M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2\"/>",
        ["task"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"3\"/><path d=\"m8 12 3 3 5-6\"/>",
        ["mail"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>",
        ["calendar"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"2\"/><path d=\"M3 10h18M8 3v4M16 3v4\"/>",
        ["upload"] = "<path d=\"M12 16V4M7 9l5-5 5 5\"/><path d=\"M4 16v4h16v-4\"/>",
        ["globe"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18\"/>",
        ["up"] = "<path d=\"m6 14 6-6 6 6\"/>",
        ["down"] = "<path d=\"m6 10 6 6 6-6\"/>",
        ["sort"] = "<path d=\"m8 9 4-4 4 4M8 15l4 4 4-4\"/>",
        ["chevron"] = "<path d=\"m9 6 6 6-6 6\"/>",
        ["bold"] = "<path d=\"M7 5h6a3.5 3.5 0 0 1 0 7H7zM7 12h7a3.5 3.5 0 0 1 0 7H7z\"/>",
        ["italic"] = "<path d=\"M10 5h8M6 19h8M14 5l-4 14\"/>",
        ["underline"] = "<path d=\"M7 4v7a5 5 0 0 0 10 0V4M5 20h14\"/>",
        ["strike"] = "<path d=\"M4 12h16M16 6.5C15 5 13.5 4.5 12 4.5c-2.5 0-4.5 1.3-4.5 3.5 0 1.3.7 2.3 2 3M8 17.5c1 1.5 2.5 2 4 2 2.5 0 4.5-1.3 4.5-3.5 0-.6-.1-1.1-.4-1.5\"/>",
        ["ul"] = "<path d=\"M9 6h11M9 12h11M9 18h11\"/><circle cx=\"4.5\" cy=\"6\" r=\"1\"/><circle cx=\"4.5\" cy=\"12\" r=\"1\"/><circle cx=\"4.5\" cy=\"18\" r=\"1\"/>",
        ["ol"] = "<path d=\"M10 6h10M10 12h10M10 18h10M4 4.5h1.5V9M4 9h3M4 14.5c0-1 2.8-1 2.8.4 0 1-2.8 2-2.8 3.6h3\"/>",
        ["link"] = "<path d=\"M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1\"/><path d=\"M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1\"/>",
        ["unlock"] = "<rect x=\"4\" y=\"11\" width=\"16\" height=\"10\" rx=\"2\"/><path d=\"M8 11V7a4 4 0 0 1 7.5-2\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19 12a7 7 0 0 0-.1-1.2l2-1.6-2-3.4-2.4 1a7 7 0 0 0-2-1.2L14 3h-4l-.5 2.6a7 7 0 0 0-2 1.2l-2.4-1-2 3.4 2 1.6a7 7 0 0 0 0 2.4l-2 1.6 2 3.4 2.4-1a7 7 0 0 0 2 1.2L10 21h4l.5-2.6a7 7 0 0 0 2-1.2l2.4 1 2-3.4-2-1.6c.1-.4.1-.8.1-1.2z\"/>",
        ["home"] = "<path d=\"M3 11 12 4l9 7\"/><path d=\"M5 10v10h14V10\"/>",
    };

    /// <summary>Renders an icon as an inline &lt;svg&gt; markup string, matching ic(name, size, strokeWidth) in the demo.</summary>
    public static string Render(string name, int size = 18, double strokeWidth = 2)
    {
        var path = Icons.GetValueOrDefault(name, string.Empty);
        return $"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{strokeWidth}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{path}</svg>";
    }
}
