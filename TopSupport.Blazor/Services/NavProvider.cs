using TopSupport.Models;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;

namespace TopSupport.Blazor.Services;

/// <summary>One row of the sidebar: either a group heading or a navigable item.</summary>
public record NavRow(string? GroupLabel, AppNavItem? Item)
{
    public static NavRow Group(string label) => new(label, null);
    public static NavRow Link(AppNavItem item) => new(null, item);
}

/// <summary>
/// Port of navItems()/workspaceNav() in topsupport-app.js, now with live nav-count badges
/// (it.count in the demo) populated from one GET /api/tickets/nav-counts round trip rather
/// than five separate list fetches. Scoped (was singleton) because it now depends on the
/// per-circuit TopSupportApiClient/HttpClient and returns per-actor counts.
/// </summary>
public class NavProvider
{
    private readonly TopSupportApiClient _api;

    public NavProvider(TopSupportApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// actorId is the signed-in account's user id (role == User) or agent id (role == Agent/
    /// Admin) -- whichever GetNavCountsAsync needs to compute MyOpenTickets/QueueMine. Pass
    /// null if unknown (counts are simply omitted/zero in that case).
    /// </summary>
    public async Task<IReadOnlyList<NavRow>> GetNavRowsAsync(AppRole role, string? actorId, CancellationToken cancellationToken = default)
    {
        TicketNavCountsDtoOrDefault counts;
        try
        {
            var userId = role == AppRole.User ? actorId : null;
            var agentId = role is AppRole.Agent or AppRole.Admin ? actorId : null;
            var dto = await _api.GetNavCountsAsync(userId, agentId, cancellationToken);
            counts = new TicketNavCountsDtoOrDefault(dto);
        }
        catch
        {
            // Nav must still render if the Api call fails (e.g. during startup) -- just without badges.
            counts = TicketNavCountsDtoOrDefault.Empty;
        }

        return role switch
        {
            AppRole.User => new[]
            {
                NavRow.Group("Help"),
                NavRow.Link(new AppNavItem { Label = "Raise a Ticket", Route = "/raise", Icon = "plus", Roles = new[] { AppRole.User } }),
                NavRow.Link(new AppNavItem { Label = "My Tickets", Route = "/my-tickets", Icon = "ticket", Roles = new[] { AppRole.User }, Count = counts.MyOpenTickets }),
                NavRow.Group("Account"),
                NavRow.Link(new AppNavItem { Label = "My Profile", Route = "/profile", Icon = "user", Roles = new[] { AppRole.User, AppRole.Agent, AppRole.Admin } }),
            },
            AppRole.Agent => WorkspaceNavRows(counts),
            AppRole.Admin => new[]
            {
                NavRow.Group("Admin"),
                NavRow.Link(new AppNavItem { Label = "Dashboard", Route = "/dashboard", Icon = "chart", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Users", Route = "/users", Icon = "users", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Organisations", Route = "/orgs", Icon = "building", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Agents and Teams", Route = "/agents", Icon = "team", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Roles and Permissions", Route = "/roles", Icon = "key", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Security", Route = "/security", Icon = "shield", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Knowledge Base", Route = "/kb", Icon = "book", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Site Pages", Route = "/site", Icon = "page", Roles = new[] { AppRole.Admin } }),
                NavRow.Link(new AppNavItem { Label = "Audit Trail", Route = "/audit", Icon = "list", Roles = new[] { AppRole.Admin } }),
            }.Concat(WorkspaceNavRows(counts)).ToArray(),
            _ => Array.Empty<NavRow>()
        };
    }

    private static IReadOnlyList<NavRow> WorkspaceNavRows(TicketNavCountsDtoOrDefault counts) => new[]
    {
        NavRow.Group("Queues"),
        NavRow.Link(new AppNavItem { Label = "Open", Route = "/queue/open", Icon = "inbox", Roles = new[] { AppRole.Agent, AppRole.Admin }, Count = counts.QueueOpen }),
        NavRow.Link(new AppNavItem { Label = "Assigned to Me", Route = "/queue/mine", Icon = "user", Roles = new[] { AppRole.Agent, AppRole.Admin }, Count = counts.QueueMine }),
        NavRow.Link(new AppNavItem { Label = "Overdue", Route = "/queue/overdue", Icon = "clock", Roles = new[] { AppRole.Agent, AppRole.Admin }, Count = counts.QueueOverdue, CountIsAlert = true }),
        NavRow.Link(new AppNavItem { Label = "Answered", Route = "/queue/answered", Icon = "reply", Roles = new[] { AppRole.Agent, AppRole.Admin }, Count = counts.QueueAnswered }),
        NavRow.Link(new AppNavItem { Label = "Closed", Route = "/queue/closed", Icon = "lock", Roles = new[] { AppRole.Agent, AppRole.Admin }, Count = counts.QueueClosed }),
        NavRow.Group("Tickets"),
        NavRow.Link(new AppNavItem { Label = "New Ticket for a User", Route = "/new-ticket", Icon = "plus", Roles = new[] { AppRole.Agent, AppRole.Admin } }),
        NavRow.Link(new AppNavItem { Label = "Canned Responses", Route = "/canned", Icon = "note", Roles = new[] { AppRole.Agent, AppRole.Admin } }),
    };

    /// <summary>Null-safe wrapper so a failed/omitted nav-counts call just means no badges, not a broken nav.</summary>
    private readonly record struct TicketNavCountsDtoOrDefault(int MyOpenTickets, int QueueOpen, int QueueMine, int QueueOverdue, int QueueAnswered, int QueueClosed)
    {
        public static readonly TicketNavCountsDtoOrDefault Empty = new();

        public TicketNavCountsDtoOrDefault(TicketNavCountsDto dto)
            : this(dto.MyOpenTickets, dto.QueueOpen, dto.QueueMine, dto.QueueOverdue, dto.QueueAnswered, dto.QueueClosed)
        {
        }
    }
}
