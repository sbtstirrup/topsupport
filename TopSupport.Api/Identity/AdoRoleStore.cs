using Microsoft.AspNetCore.Identity;

namespace TopSupport.Api.Identity;

/// <summary>
/// Skeleton ADO.NET-backed role store. See AdoUserStore for why this exists instead of
/// Identity's default EF Core RoleStore. NOT IMPLEMENTED YET: every member throws.
/// </summary>
public class AdoRoleStore : IRoleStore<ApplicationRole>
{
    private static NotImplementedException NotYetImplemented([System.Runtime.CompilerServices.CallerMemberName] string? member = null)
        => new($"{member} is not implemented yet: AdoRoleStore is a skeleton pending ADO.NET-backed Identity storage. See TopSupport.Database/README.md.");

    public void Dispose() { }

    public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) => throw NotYetImplemented();
}
