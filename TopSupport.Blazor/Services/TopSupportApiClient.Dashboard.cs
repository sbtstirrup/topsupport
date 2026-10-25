using System.Net.Http.Json;
using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

public partial class TopSupportApiClient
{
    // ---------------- Dashboard (DashboardController) ----------------

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<DashboardDto>("api/dashboard", cancellationToken);
}
