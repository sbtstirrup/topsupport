using TopSupport.Models.Admin;

namespace TopSupport.Services.Abstractions;

/// <summary>Data-access contract for dbo.AuditEntries, calling usp_Audit_Insert/GetRecent.</summary>
public interface IAuditRepository
{
    Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(CancellationToken cancellationToken = default);

    Task InsertAsync(AuditEntryDto entry, CancellationToken cancellationToken = default);
}
