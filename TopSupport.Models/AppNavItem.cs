using TopSupport.Models.Enums;

namespace TopSupport.Models;

/// <summary>
/// Describes one entry in the sidebar navigation, mirroring navItems()/workspaceNav() in
/// topsupport-app.js: a route, a label, an icon key, and which roles can see it.
/// </summary>
public class AppNavItem
{
    public string Label { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public IReadOnlyCollection<AppRole> Roles { get; set; } = Array.Empty<AppRole>();

    /// <summary>Optional nav-badge count (the demo's it.count), populated from
    /// GET /api/tickets/nav-counts by NavProvider.GetNavRowsAsync. Null hides the badge.</summary>
    public int? Count { get; set; }

    /// <summary>True to render the count badge in its "alert" (red) style when non-zero --
    /// mirrors the demo's it.alert flag used by the Overdue queue nav item.</summary>
    public bool CountIsAlert { get; set; }
}
