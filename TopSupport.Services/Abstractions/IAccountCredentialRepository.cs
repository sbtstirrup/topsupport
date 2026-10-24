namespace TopSupport.Services.Abstractions;

/// <summary>Stored credential for one account (a Users or Agents row).</summary>
public record AccountCredential(string AccountType, string? PasswordHash, string? AgentRole);

/// <summary>Data-access contract for account password hashes, calling usp_Account_GetPasswordHash/SetPasswordHash.</summary>
public interface IAccountCredentialRepository
{
    /// <summary>Null when no Users or Agents row has this id.</summary>
    Task<AccountCredential?> GetAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>Stores the hash and unlocks the account.</summary>
    Task SetPasswordHashAsync(string accountId, string passwordHash, CancellationToken cancellationToken = default);
}
