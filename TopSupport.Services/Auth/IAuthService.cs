using TopSupport.Models.Enums;

namespace TopSupport.Services.Auth;

/// <summary>Result of a successful sign-in: which account signed in and which portal/role it lands in.</summary>
public record SignInResult(string AccountId, AppRole Role, string DisplayName);

/// <summary>
/// Business-facing auth operations consumed by TopSupport.Api's AuthController. Backed by
/// ASP.NET Core Identity (see TopSupport.Api/Identity) rather than the repository layer
/// used for domain data, since Identity owns its own user/role model.
/// </summary>
public interface IAuthService
{
    Task<SignInResult?> SignInAsync(string username, string password, CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
