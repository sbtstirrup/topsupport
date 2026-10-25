using TopSupport.Models.Admin;
using TopSupport.Models.Tickets;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Tickets;

namespace TopSupport.Services.Admin;

/// <summary>
/// Port of PAGES.dashboard's render() in topsupport-app.js (~line 2012-2045): stat tiles, the
/// 13-week opened/closed series, department/topic breakdowns, per-agent workload and the
/// "Overdue Now" list. Loads every ticket once via usp_Ticket_SearchForQueue (which carries
/// FirstAgentReplyAt) plus closedAt from usp_Dashboard_GetTicketClosedAt, then computes in
/// C# using TicketSla so active/overdue/SLA-due match the queue exactly.
/// </summary>
public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public class DashboardService : IDashboardService
{
    private const int WeekCount = 13;

    private readonly ITicketRepository _tickets;
    private readonly IDashboardRepository _dashboard;
    private readonly IAgentRepository _agents;
    private readonly ITeamRepository _teams;
    private readonly IUserRepository _users;
    private readonly TimeProvider _time;

    public DashboardService(
        ITicketRepository tickets,
        IDashboardRepository dashboard,
        IAgentRepository agents,
        ITeamRepository teams,
        IUserRepository users,
        TimeProvider time)
    {
        _tickets = tickets;
        _dashboard = dashboard;
        _agents = agents;
        _teams = teams;
        _users = users;
        _time = time;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var all = await _tickets.SearchForQueueAsync(null, null, null, null, null, null, null, cancellationToken);
        var closedAtById = await _dashboard.GetTicketClosedAtAsync(cancellationToken);
        var agents = await _agents.GetAllActiveAsync(cancellationToken);
        var teams = await _teams.GetAllAsync(cancellationToken);
        var users = await _users.GetAllActiveAsync(cancellationToken);

        // closedAt(t): null while active, matching the demo (a reopened ticket's old close
        // event must not count).
        DateTimeOffset? ClosedAt(TicketDto t) =>
            !TicketSla.IsActive(t) && closedAtById.TryGetValue(t.Id, out var at) ? at : null;

        var now = _time.GetUtcNow();
        var week = TimeSpan.FromDays(7);
        var last30 = now.AddDays(-30);

        var result = new DashboardDto
        {
            ActiveCount = all.Count(TicketSla.IsActive),
            OverdueCount = all.Count(t => TicketSla.IsOverdue(t, now)),
            OpenedLast30Days = all.Count(t => t.Created >= last30),
            ClosedLast30Days = all.Count(t => ClosedAt(t) is { } c && c >= last30),
            AvgFirstResponseHours = AvgHours(all)
        };

        var responded = all.Where(TicketSla.HasFirstResponse).ToList();
        result.RespondedCount = responded.Count;
        result.ResponseTargetMetCount = responded.Count(t => t.FirstAgentReplyAt <= TicketSla.SlaDue(t));

        for (var i = WeekCount - 1; i >= 0; i--)
        {
            var start = now - (i + 1) * week;
            var end = now - i * week;
            result.Weeks.Add(new DashboardWeekDto
            {
                Start = start,
                Opened = all.Count(t => t.Created >= start && t.Created < end),
                Closed = all.Count(t => ClosedAt(t) is { } c && c >= start && c < end)
            });
        }

        foreach (var dept in TicketTopics.DEPTS)
        {
            var inDept = all.Where(t => t.Department == dept).ToList();
            result.Departments.Add(new DashboardDepartmentDto
            {
                Department = dept,
                Active = inDept.Count(TicketSla.IsActive),
                Overdue = inDept.Count(t => TicketSla.IsOverdue(t, now)),
                AvgFirstResponseHours = AvgHours(inDept)
            });
        }

        foreach (var topic in TicketTopics.TOPICS)
        {
            result.Topics.Add(new DashboardTopicDto { Label = topic.Label, Count = all.Count(t => t.Topic == topic.Id) });
        }

        foreach (var agent in agents)
        {
            var mine = all.Where(t => t.AssignedAgentId == agent.Id).ToList();
            result.Agents.Add(new DashboardAgentDto
            {
                AgentId = agent.Id,
                Name = agent.Name,
                TeamNames = string.Join(", ", agent.TeamIds.Select(id => teams.FirstOrDefault(t => t.Id == id)?.Name ?? string.Empty)),
                Active = mine.Count(TicketSla.IsActive),
                ResolvedOrClosed = mine.Count(t => !TicketSla.IsActive(t)),
                AvgFirstResponseHours = AvgHours(mine),
                Overdue = mine.Count(t => TicketSla.IsOverdue(t, now))
            });
        }

        result.UnassignedActive = all.Count(t => TicketSla.IsActive(t) && t.AssignedAgentId == null);
        result.UnassignedOverdue = all.Count(t => TicketSla.IsOverdue(t, now) && t.AssignedAgentId == null);

        // DB.tickets order in the demo is creation order (oldest first).
        foreach (var t in all.Where(t => TicketSla.IsOverdue(t, now)).OrderBy(t => t.Created))
        {
            var requester = users.FirstOrDefault(u => u.Id == t.RequesterUserId)
                ?? await _users.GetByIdAsync(t.RequesterUserId, cancellationToken);

            result.OverdueTickets.Add(new DashboardOverdueTicketDto
            {
                Id = t.Id,
                Number = t.Number,
                Subject = t.Subject,
                Priority = t.Priority,
                RequesterName = requester?.Name ?? t.RequesterUserId,
                AssigneeLabel = t.AssignedAgentId != null
                    ? agents.FirstOrDefault(a => a.Id == t.AssignedAgentId)?.Name ?? string.Empty
                    : t.AssignedTeamId != null
                        ? teams.FirstOrDefault(tm => tm.Id == t.AssignedTeamId)?.Name ?? string.Empty
                        : "Unassigned",
                SlaDue = TicketSla.SlaDue(t)
            });
        }

        return result;
    }

    /// <summary>avgHours(ts): mean hours from creation to first agent reply over the responded tickets.</summary>
    private static double? AvgHours(IEnumerable<TicketDto> tickets)
    {
        var hours = tickets
            .Where(t => t.FirstAgentReplyAt.HasValue)
            .Select(t => (t.FirstAgentReplyAt!.Value - t.Created).TotalHours)
            .ToList();
        return hours.Count > 0 ? hours.Average() : null;
    }
}
