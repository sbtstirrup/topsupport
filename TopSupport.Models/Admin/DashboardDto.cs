using TopSupport.Models.Enums;

namespace TopSupport.Models.Admin;

/// <summary>
/// Everything PAGES.dashboard in topsupport-app.js renders, pre-computed server-side by
/// DashboardService so the Blazor page is a pure renderer. Hour averages are null when there
/// is nothing to average (the demo's avgHours returning null, shown as "—" by fmtH).
/// </summary>
public class DashboardDto
{
    // ---- stat tiles ----
    public int ActiveCount { get; set; }
    public int OverdueCount { get; set; }
    public int OpenedLast30Days { get; set; }
    public int ClosedLast30Days { get; set; }
    public double? AvgFirstResponseHours { get; set; }

    /// <summary>Tickets that have had a first agent response (the "of N responded tickets" denominator).</summary>
    public int RespondedCount { get; set; }

    /// <summary>Responded tickets whose first response landed on or before their SLA due time.</summary>
    public int ResponseTargetMetCount { get; set; }

    // ---- weekly opened/closed line chart (oldest week first) ----
    public List<DashboardWeekDto> Weeks { get; set; } = new();

    public List<DashboardDepartmentDto> Departments { get; set; } = new();
    public List<DashboardTopicDto> Topics { get; set; } = new();
    public List<DashboardAgentDto> Agents { get; set; } = new();

    /// <summary>Active tickets with no individual agent (the "Unassigned (team queue)" row).</summary>
    public int UnassignedActive { get; set; }
    public int UnassignedOverdue { get; set; }

    public List<DashboardOverdueTicketDto> OverdueTickets { get; set; } = new();
}

public class DashboardWeekDto
{
    /// <summary>Start of the 7-day window; the label shows this date (the demo's start + 1h).</summary>
    public DateTimeOffset Start { get; set; }
    public int Opened { get; set; }
    public int Closed { get; set; }
}

public class DashboardDepartmentDto
{
    public string Department { get; set; } = string.Empty;
    public int Active { get; set; }
    public int Overdue { get; set; }
    public double? AvgFirstResponseHours { get; set; }
}

public class DashboardTopicDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardAgentDto
{
    public string AgentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Comma-separated team names (the demo's a.teams.map(team name).join(', ')).</summary>
    public string TeamNames { get; set; } = string.Empty;

    public int Active { get; set; }
    public int ResolvedOrClosed { get; set; }
    public double? AvgFirstResponseHours { get; set; }
    public int Overdue { get; set; }
}

public class DashboardOverdueTicketDto
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public string RequesterName { get; set; } = string.Empty;

    /// <summary>assigneeLabel(t): agent name, else team name, else "Unassigned".</summary>
    public string AssigneeLabel { get; set; } = string.Empty;

    public DateTimeOffset SlaDue { get; set; }
}
