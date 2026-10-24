namespace TopSupport.Models.Tickets;

/// <summary>
/// Request shape for IQueueService.SearchAsync, combining the 5 built-in queues (open/mine/
/// overdue/answered/closed from the demo's QUEUES map) with free filtering/sorting/paging
/// (BLANK_FILTERS/applyFilters/thSort/pager in topsupport-app.js).
/// </summary>
public class TicketQueueRequest
{
    /// <summary>"open" | "mine" | "overdue" | "answered" | "closed" | null for a plain filtered search.</summary>
    public string? Queue { get; set; }

    /// <summary>Required when Queue == "mine" (or when filtering by "assigned to me").</summary>
    public string? ActingAgentId { get; set; }

    public string? SearchText { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Topic { get; set; }
    public string? Department { get; set; }

    /// <summary>"me" | "none" | "a:{agentId}" | "t:{teamId}" — mirrors the demo's assignee filter values.</summary>
    public string? Assignee { get; set; }

    /// <summary>num/subject/requester/affected/topic/priority/sla/assignee/status/created.</summary>
    public string? SortKey { get; set; }
    public bool SortDescending { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

/// <summary>Paged result returned by IQueueService.SearchAsync.</summary>
public class TicketQueuePageResult
{
    public List<TicketDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
