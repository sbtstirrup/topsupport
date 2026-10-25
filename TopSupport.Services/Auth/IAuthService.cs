using TopSupport.Models.Enums;

namespace TopSupport.Services.Auth;

/// <summary>Result of a successful sign-in: which account signed in and which portal/role it lands in.</summary>
public record SignInResult(string AccountId, AppRole Role, string DisplayName);

/// <summary>Either <see cref="Account"/> (signed in) or <see cref="Error"/> (the message to show on the sign-in page).</summary>
public record SignInOutcome(SignInResult? Account, string? Error);

/// <summary>
/// One account in the development-only test switcher. <see cref="Detail"/> is the user's
/// department or the agent's departments; <see cref="CanSignIn"/> is false when locked or inactive.
/// </summary>
public record TestAccountItem(string AccountId, AppRole Role, string Name, string Username, string? AgentRole, string? Detail, bool CanSignIn, string? Status);

/// <summary>
/// Port of ACT.signin / ACT.signout in topsupport-app.js against the Users/Agents rows and their
/// PasswordHash. Issuing the token that proves the sign-in is the Api's job (JwtTokenService).
/// </summary>
public interface IAuthService
{
    Task<SignInOutcome> SignInAsync(string? username, string? password, CancellationToken cancellationToken = default);

    /// <summary>Audits the sign-out; the token itself simply expires (the Blazor app drops its cookie).</summary>
    Task SignOutAsync(string accountId, AppRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Development-only test switcher: signs in as any account without its password. Locked and
    /// inactive accounts still can't sign in, so the switcher shows real behaviour. The Api only
    /// calls this when the switcher is enabled (Development + Auth:TestAccountSwitcher:Enabled).
    /// </summary>
    Task<SignInOutcome> TestSignInAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>The accounts the test switcher lists, with the role each signs in as.</summary>
    Task<IReadOnlyList<TestAccountItem>> ListTestAccountsAsync(CancellationToken cancellationToken = default);
}
