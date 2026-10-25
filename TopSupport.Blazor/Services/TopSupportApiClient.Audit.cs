using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

// Admin features each add their own TopSupportApiClient.<Feature>.cs partial file like this one,
// so the typed client stays one class without every feature editing the same file.
public partial class TopSupportApiClient
{
    // ---------------- Audit (AuditController) ----------------

    public async Task<IReadOnlyList<AuditEntryDto>> GetAuditAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<AuditEntryDto>>("api/audit", cancellationToken);
}
