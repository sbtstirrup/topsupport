using TopSupport.Models.Enums;

namespace TopSupport.Services.Auth;

/// <summary>
/// Skeleton implementation. Real sign-in against ASP.NET Core Identity's SignInManager/
/// UserManager is wired in TopSupport.Api once the custom ADO.NET-backed user store
/// (TopSupport.Api/Identity) is implemented — see TopSupport.Database/README.md for why
/// Identity's default EF store isn't used here.
/// </summary>
public class AuthService : IAuthService
{
    public Task<SignInResult?> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "Sign-in is not implemented in this skeleton pass: it depends on the ADO.NET-backed " +
            "Identity user store (TopSupport.Api/Identity/AdoUserStore.cs) being completed first.");

    public Task SignOutAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
