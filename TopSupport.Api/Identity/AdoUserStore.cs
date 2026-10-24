using Microsoft.AspNetCore.Identity;

namespace TopSupport.Api.Identity;

/// <summary>
/// Skeleton ADO.NET-backed user store for ASP.NET Core Identity, standing in for Identity's
/// usual EF Core UserStore (which this PoC can't use, since all data access here is raw
/// ADO.NET + stored procedures — see TopSupport.Database/README.md).
///
/// NOT IMPLEMENTED YET: every member throws. This exists so Program.cs can register
/// Identity's services and the solution compiles end to end; implementing each method
/// against dbo.AspNetUsers via stored procedures is the next real task before sign-in works.
/// </summary>
public class AdoUserStore :
    IUserStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>
{
    private static NotImplementedException NotYetImplemented([System.Runtime.CompilerServices.CallerMemberName] string? member = null)
        => new($"{member} is not implemented yet: AdoUserStore is a skeleton pending ADO.NET-backed Identity storage. See TopSupport.Database/README.md.");

    public void Dispose() { }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw NotYetImplemented();

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();

    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) => throw NotYetImplemented();

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotYetImplemented();
    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken) => throw NotYetImplemented();
}
