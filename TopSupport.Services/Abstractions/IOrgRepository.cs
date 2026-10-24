using TopSupport.Models.Admin;
using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.Orgs, calling the usp_Org_* procs in
/// TopSupport.Database/Scripts/08_Admin_UsersOrgs.sql.
/// </summary>
public interface IOrgRepository
{
    /// <summary>Every organisation with member and open-ticket counts, ordered by name.</summary>
    Task<IReadOnlyList<OrgListItemDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<OrgDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Ids of active tickets where a member is requester or affected user, newest first.</summary>
    Task<IReadOnlyList<string>> GetOpenTicketIdsAsync(string orgId, CancellationToken cancellationToken = default);

    Task InsertAsync(OrgDto org, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrgDto org, CancellationToken cancellationToken = default);
}
