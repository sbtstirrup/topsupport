namespace TopSupport.Services.Abstractions;

/// <summary>Stored credential for one account (a Users or Agents row).</summary>
public record AccountCredential(string AccountType, string? PasswordHash, string? AgentRole);

/// <summary>The account a username signs in as (usp_Account_GetByUsername).</summary>
public record SignInAccount(string AccountType, string Id, string Name, string? AgentRole, string? PasswordHash, bool Locked, bool Active);

/// <summary>An account's current state (usp_Account_GetStatus), checked on every authenticated Api request.</summary>
public record AccountStatus(string AccountType, string? AgentRole, bool Locked, bool Active);

/// <summary>An account the development-only test switcher can sign in as (usp_Account_ListForTesting).</summary>
public record TestAccount(string AccountType, string Id, string Name, string Username, string Email, string? AgentRole, string? Detail, bool Locked, bool Active);

/// <summary>Data-access contract for account password hashes and sign-in state (07_ and 13_ scripts).</summary>
public interface IAccountCredentialRepository
{
    /// <summary>Null when no Users or Agents row has this id.</summary>
    Task<AccountCredential?> GetAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>Stores the hash; <paramref name="unlock"/> also clears Locked (admin resets only).</summary>
    Task SetPasswordHashAsync(string accountId, string passwordHash, bool unlock, CancellationToken cancellationToken = default);

    /// <summary>Null when no non-deleted user or agent has this username.</summary>
    Task<SignInAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Counts a failed sign-in, locking at <paramref name="lockAfter"/> (0 = never); true when this call locked it.</summary>
    Task<bool> RecordFailedSignInAsync(string accountId, int lockAfter, CancellationToken cancellationToken = default);

    Task ResetFailedSignInsAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>Null when no Users or Agents row has this id.</summary>
    Task<AccountStatus?> GetStatusAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>Every non-deleted user and every agent, for the development-only test switcher.</summary>
    Task<IReadOnlyList<TestAccount>> ListForTestingAsync(CancellationToken cancellationToken = default);
}
