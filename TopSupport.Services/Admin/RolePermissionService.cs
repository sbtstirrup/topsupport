using TopSupport.Models.Admin;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// The per-department role permission grid (DB.perms / PAGES.roles in topsupport-app.js). The demo
/// renders the grid read-only but its audit seed records changes as 'Changed permission "Delete"
/// for Agent role' against the department, so SetAsync audits with that wording.
/// </summary>
public class RolePermissionService
{
    private readonly IRolePermissionRepository _repository;
    private readonly AuditService _audit;

    public RolePermissionService(IRolePermissionRepository repository, AuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public Task<IReadOnlyList<RolePermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <summary>Throws ValidationException for an unknown department, role or permission.</summary>
    public async Task SetAsync(SetRolePermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (!TicketTopics.DEPTS.Contains(request.Department)) throw new ValidationException("department", $"Unknown department \"{request.Department}\".");
        if (!AgentRoles.All.Contains(request.Role)) throw new ValidationException("role", $"Unknown role \"{request.Role}\".");
        var permission = RolePermissionKeys.All.FirstOrDefault(p => p.Key == request.Permission);
        if (permission.Key is null) throw new ValidationException("permission", $"Unknown permission \"{request.Permission}\".");

        await _repository.SetAsync(request.Department, request.Role, permission.Key, request.Allowed, cancellationToken);
        await _audit.LogAsync(request.ActorId,
            "Changed permission \"" + permission.Label + "\" for " + request.Role + " role",
            request.Department, AuditCategories.Permission, cancellationToken);
    }
}
