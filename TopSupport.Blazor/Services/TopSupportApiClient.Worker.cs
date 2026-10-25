using System.Net.Http.Json;
using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

public partial class TopSupportApiClient
{
    // ---------------- Background jobs (WorkerMonitorController) ----------------

    public async Task<WorkerJobsDto> GetWorkerJobsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<WorkerJobsDto>("api/admin/worker/jobs", cancellationToken) ?? new();

    public async Task<WorkerNotificationsDto> GetWorkerNotificationsAsync(string? type, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/worker/notifications?page={page}&pageSize={pageSize}"
            + (string.IsNullOrEmpty(type) ? string.Empty : "&type=" + Uri.EscapeDataString(type))
            + (string.IsNullOrWhiteSpace(search) ? string.Empty : "&search=" + Uri.EscapeDataString(search.Trim()));
        return await _httpClient.GetFromJsonAsync<WorkerNotificationsDto>(query, cancellationToken) ?? new();
    }
}
