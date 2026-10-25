using TopSupport.Models.Admin;
using TopSupport.Models.Enums;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Services.Auth;

/// <summary>
/// ACT.signin with the demo's messages and audit wording, plus the lockout the demo only stores
/// as a setting: each wrong password counts against the account, and it locks at the security
/// setting "lockoutAttempts" (0 turns lockout off). An admin unlock or password reset clears it.
/// </summary>
public class AuthService : IAuthService
{
    private const string WrongCredentials = "The username or password is not right. Check them and try again.";
    private const string LockedMessage = "This account is locked. Email itsupport@topcashback.example to unlock it.";

    private readonly IAccountCredentialRepository _credentials;
    private readonly IAccountPasswordService _passwords;
    private readonly ISettingsService _settings;
    private readonly IAuditService _audit;

    public AuthService(IAccountCredentialRepository credentials, IAccountPasswordService passwords, ISettingsService settings, IAuditService audit)
    {
        _credentials = credentials;
        _passwords = passwords;
        _settings = settings;
        _audit = audit;
    }

    public async Task<SignInOutcome> SignInAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        var un = (username ?? string.Empty).Trim().ToLowerInvariant();
        if (un.Length == 0 || string.IsNullOrEmpty(password)) return Fail("Enter your username and password.");

        // Usernames are at most 20 characters; anything longer can't match, and the length caps
        // keep the audit text and the PBKDF2 input bounded.
        var account = un.Length <= 100 && password.Length <= FieldLimits.Password
            ? await _credentials.GetByUsernameAsync(un, cancellationToken)
            : null;

        // An inactive agent can't sign in; treated as unknown so it doesn't reveal the account.
        if (account is null || !account.Active || !await _passwords.VerifyAsync(account.Id, password, cancellationToken))
        {
            await _audit.LogAsync(account?.Id ?? "unknown", "Failed sign-in (" + Truncate(un, 100) + ")", "Sign-in page", AuditCategories.SignIn, cancellationToken);

            if (account is not null && !account.Locked)
            {
                var lockAfter = (await _settings.GetSecurityAsync(cancellationToken)).LockoutAttempts;
                if (await _credentials.RecordFailedSignInAsync(account.Id, lockAfter, cancellationToken))
                {
                    await _audit.LogAsync(account.Id, $"Locked account after {lockAfter} failed sign-ins", account.Name, AuditCategories.SignIn, cancellationToken);
                }
            }

            return Fail(WrongCredentials);
        }

        // As the demo, the lock is only revealed to someone who knows the password.
        if (account.Locked) return Fail(LockedMessage);

        await _credentials.ResetFailedSignInsAsync(account.Id, cancellationToken);

        var role = RoleOf(account.AccountType, account.AgentRole);
        await _audit.LogAsync(account.Id, "Signed in", PortalOf(role), AuditCategories.SignIn, cancellationToken);
        return new SignInOutcome(new SignInResult(account.Id, role, account.Name), null);
    }

    public Task SignOutAsync(string accountId, AppRole role, CancellationToken cancellationToken = default)
        => _audit.LogAsync(accountId, "Signed out", PortalOf(role), AuditCategories.SignIn, cancellationToken);

    public async Task<SignInOutcome> TestSignInAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var account = (await _credentials.ListForTestingAsync(cancellationToken)).FirstOrDefault(a => a.Id == accountId);
        if (account is null) return Fail("That test account doesn't exist.");
        if (!account.Active) return Fail("That account is inactive, so it can't sign in.");
        if (account.Locked) return Fail(LockedMessage);

        var role = RoleOf(account.AccountType, account.AgentRole);
        await _audit.LogAsync(account.Id, "Signed in (test account switcher)", PortalOf(role), AuditCategories.SignIn, cancellationToken);
        return new SignInOutcome(new SignInResult(account.Id, role, account.Name), null);
    }

    public async Task<IReadOnlyList<TestAccountItem>> ListTestAccountsAsync(CancellationToken cancellationToken = default)
        => (await _credentials.ListForTestingAsync(cancellationToken))
            .Select(a => new TestAccountItem(
                a.Id, RoleOf(a.AccountType, a.AgentRole), a.Name, a.Username, a.AgentRole, a.Detail,
                a.Active && !a.Locked,
                !a.Active ? "Inactive" : a.Locked ? "Locked" : null))
            .ToList();

    /// <summary>signInAs(acct) in the demo: users get the portal, Administrator agents the admin console.</summary>
    public static AppRole RoleOf(string accountType, string? agentRole)
        => accountType == "User" ? AppRole.User : agentRole == "Administrator" ? AppRole.Admin : AppRole.Agent;

    private static SignInOutcome Fail(string error) => new(null, error);

    private static string PortalOf(AppRole role) => role == AppRole.User ? "User portal" : "Agent portal";

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
