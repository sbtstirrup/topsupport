using System.Text.Json;
using TopSupport.Models.Admin;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Tickets;

/// <summary>
/// The agent queues. Given PoC-scale data, loads the active/filtered set from
/// ITicketRepository then applies queue membership (QUEUES), free-text/status/priority/
/// topic/department/assignee filters (applyFilters), sorting (thSort keys) and paging (pager)
/// entirely in C#, matching the demo's client-side logic rather than duplicating it in T-SQL.
/// </summary>
public class QueueService
{
    /// <summary>Largest page the queue serves (the agent UI asks for 15; DevicePanel's lookup for up to 200 gets 100).</summary>
    public const int MaxPageSize = 100;

    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly TimeProvider _time;

    public QueueService(ITicketRepository tickets, IUserRepository users, TimeProvider time)
    {
        _tickets = tickets;
        _users = users;
        _time = time;
    }

    public async Task<TicketQueuePageResult> SearchAsync(TicketQueueRequest request, string actingAgentId, CancellationToken cancellationToken = default)
    {
        // Free text is capped at FieldLimits.SearchText (the search box's maxlength) -- longer input is truncated.
        var searchText = request.SearchText?.Trim();
        if (searchText?.Length > FieldLimits.SearchText) searchText = searchText[..FieldLimits.SearchText].TrimEnd();
        if (string.IsNullOrEmpty(searchText)) searchText = null;

        var now = _time.GetUtcNow();
        IReadOnlyList<TicketDto> source;

        // The 4 active-derived queues (open/mine/overdue/answered), and a plain unfiltered
        // search (no Queue specified), all start from the full active set so overdue/answered/
        // mine can be computed client-side. "closed" and any other filtered combination use
        // the filtered proc instead, since the active set never contains closed tickets.
        if (request.Queue is "open" or "mine" or "overdue" or "answered" or null)
        {
            source = await _tickets.GetActiveForQueueAsync(cancellationToken);
        }
        else
        {
            source = await _tickets.SearchForQueueAsync(
                request.Queue == "closed" ? null : request.Status,
                request.Priority,
                request.Topic,
                request.Department,
                AssignedAgentIdFilter(request.Assignee),
                AssignedTeamIdFilter(request.Assignee),
                searchText,
                cancellationToken);
        }

        IEnumerable<TicketDto> filtered = request.Queue switch
        {
            "open" => source.Where(TicketSla.IsActive),
            "mine" => source.Where(t => TicketSla.IsActive(t) && t.AssignedAgentId == actingAgentId),
            "overdue" => source.Where(t => TicketSla.IsOverdue(t, now)),
            "answered" => source.Where(TicketSla.IsAnswered),
            "closed" => source.Where(t => !TicketSla.IsActive(t)),
            _ => source
        };

        // Requester/affected names for the free-text match (applyFilters searches r.name/a.name too).
        var userNames = searchText is null
            ? new Dictionary<string, string>()
            : (await _users.GetAllActiveAsync(cancellationToken)).ToDictionary(u => u.Id, u => u.Name);

        filtered = ApplyFilters(filtered, request, actingAgentId, searchText, userNames);

        var sorted = ApplySort(filtered, request.SortKey, request.SortDescending).ToList();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        // long arithmetic: a huge Page must not overflow into a negative Skip.
        var skip = (int)Math.Min(int.MaxValue, ((long)page - 1) * pageSize);
        var items = sorted.Skip(skip).Take(pageSize).ToList();

        return new TicketQueuePageResult
        {
            Items = items,
            TotalCount = sorted.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>Backs GET /api/tickets/nav-counts.</summary>
    public async Task<TicketNavCountsDto> GetNavCountsAsync(string? userId, string? agentId, CancellationToken cancellationToken = default)
    {
        var result = new TicketNavCountsDto();

        if (!string.IsNullOrEmpty(userId))
        {
            var mine = await _tickets.GetByRequesterOrAffectedAsync(userId, cancellationToken);
            result.MyOpenTickets = mine.Count(TicketSla.IsActive);
        }

        var active = await _tickets.GetActiveForQueueAsync(cancellationToken);
        result.QueueOpen = active.Count(TicketSla.IsActive);
        var now = _time.GetUtcNow();
        result.QueueOverdue = active.Count(t => TicketSla.IsOverdue(t, now));
        result.QueueAnswered = active.Count(TicketSla.IsAnswered);
        if (!string.IsNullOrEmpty(agentId))
        {
            result.QueueMine = active.Count(t => TicketSla.IsActive(t) && t.AssignedAgentId == agentId);
        }

        var closed = await _tickets.SearchForQueueAsync(null, null, null, null, null, null, null, cancellationToken);
        result.QueueClosed = closed.Count(t => !TicketSla.IsActive(t));

        return result;
    }

    private static string? AssignedAgentIdFilter(string? assignee) =>
        assignee != null && assignee.StartsWith("a:", StringComparison.Ordinal) ? assignee[2..] : null;

    private static string? AssignedTeamIdFilter(string? assignee) =>
        assignee != null && assignee.StartsWith("t:", StringComparison.Ordinal) ? assignee[2..] : null;

    private static IEnumerable<TicketDto> ApplyFilters(IEnumerable<TicketDto> list, TicketQueueRequest request, string actingAgentId, string? searchText, IReadOnlyDictionary<string, string> userNames)
    {
        var q = (searchText ?? string.Empty).ToLowerInvariant();

        return list.Where(t =>
        {
            if (!string.IsNullOrEmpty(request.Status) && t.Status.ToString() != request.Status) return false;
            if (!string.IsNullOrEmpty(request.Priority) && t.Priority.ToString() != request.Priority) return false;
            if (!string.IsNullOrEmpty(request.Topic) && t.Topic != request.Topic) return false;
            if (!string.IsNullOrEmpty(request.Department) && t.Department != request.Department) return false;

            if (!string.IsNullOrEmpty(request.Assignee))
            {
                if (request.Assignee == "me" && t.AssignedAgentId != actingAgentId) return false;
                if (request.Assignee == "none" && t.AssignedAgentId != null) return false;
                if (request.Assignee.StartsWith("a:", StringComparison.Ordinal) && t.AssignedAgentId != request.Assignee[2..]) return false;
                if (request.Assignee.StartsWith("t:", StringComparison.Ordinal) && t.AssignedTeamId != request.Assignee[2..]) return false;
            }

            if (q.Length > 0)
            {
                // Fields come back from the repository's JSON deserialization as JsonElement, not
                // string -- read string elements (the demo only searches string answers).
                var fieldText = t.Fields.Values.Select(v => v switch
                {
                    string s => s,
                    JsonElement { ValueKind: JsonValueKind.String } je => je.GetString(),
                    _ => null
                }).Where(s => !string.IsNullOrEmpty(s));
                var hay = ("#" + t.Number + " " + t.Subject + " " +
                           userNames.GetValueOrDefault(t.RequesterUserId) + " " +
                           userNames.GetValueOrDefault(t.AffectedUserId) + " " +
                           string.Join(' ', fieldText))
                    .ToLowerInvariant();
                if (!hay.Contains(q)) return false;
            }

            return true;
        });
    }

    private static IEnumerable<TicketDto> ApplySort(IEnumerable<TicketDto> list, string? sortKey, bool descending)
    {
        Func<TicketDto, object> keySelector = sortKey switch
        {
            "num" => t => t.Number,
            "subject" => t => t.Subject,
            "requester" => t => t.RequesterUserId,
            "affected" => t => t.AffectedUserId,
            "topic" => t => t.Topic,
            "priority" => t => t.Priority,
            "sla" => t => TicketSla.SlaDue(t),
            "assignee" => t => t.AssignedAgentId ?? t.AssignedTeamId ?? string.Empty,
            "status" => t => t.Status,
            "created" => t => t.Created,
            _ => t => t.Created
        };

        // No explicit sort key defaults to newest-first, matching the queue's default order.
        var effectiveDescending = sortKey == null ? true : descending;
        return effectiveDescending ? list.OrderByDescending(keySelector) : list.OrderBy(keySelector);
    }
}
