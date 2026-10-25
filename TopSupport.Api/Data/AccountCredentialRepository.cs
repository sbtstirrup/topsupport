using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IAccountCredentialRepository.</summary>
public class AccountCredentialRepository : IAccountCredentialRepository
{
    private readonly IDbExecutor _db;

    public AccountCredentialRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<string?> GetPasswordHashAsync(string accountId, CancellationToken cancellationToken = default)
    {
        string? hash = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_GetPasswordHash",
            cmd => cmd.Parameters.AddWithValue("@AccountId", accountId),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    hash = reader["PasswordHash"] as string;
                }
            });

        return hash;
    }

    public Task SetPasswordHashAsync(string accountId, string passwordHash, bool unlock, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Account_SetPasswordHash",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Unlock", unlock);
            },
            cancellationToken);

    public async Task<SignInAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        SignInAccount? account = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_GetByUsername",
            cmd => cmd.Parameters.AddWithValue("@Username", username),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    account = new SignInAccount(
                        (string)reader["AccountType"],
                        (string)reader["Id"],
                        (string)reader["Name"],
                        reader["AgentRole"] as string,
                        reader["PasswordHash"] as string,
                        (bool)reader["Locked"],
                        (bool)reader["Active"]);
                }
            });

        return account;
    }

    public async Task<bool> RecordFailedSignInAsync(string accountId, int lockAfter, CancellationToken cancellationToken = default)
    {
        var justLocked = false;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_RecordFailedSignIn",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@LockAfter", lockAfter);
            },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct)) justLocked = (bool)reader["JustLocked"];
            });

        return justLocked;
    }

    public async Task<AccountStatus?> GetStatusAsync(string accountId, CancellationToken cancellationToken = default)
    {
        AccountStatus? status = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_GetStatus",
            cmd => cmd.Parameters.AddWithValue("@AccountId", accountId),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    status = new AccountStatus(
                        (string)reader["AccountType"],
                        reader["AgentRole"] as string,
                        (bool)reader["Locked"],
                        (bool)reader["Active"]);
                }
            });

        return status;
    }

    public async Task<IReadOnlyList<TestAccount>> ListForTestingAsync(CancellationToken cancellationToken = default)
    {
        var accounts = new List<TestAccount>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_ListForTesting",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    accounts.Add(new TestAccount(
                        (string)reader["AccountType"],
                        (string)reader["Id"],
                        (string)reader["Name"],
                        (string)reader["Username"],
                        (string)reader["Email"],
                        reader["AgentRole"] as string,
                        reader["Detail"] as string,
                        (bool)reader["Locked"],
                        (bool)reader["Active"]));
                }
            });

        return accounts;
    }

    public Task ResetFailedSignInsAsync(string accountId, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Account_ResetFailedSignIns",
            cmd => cmd.Parameters.AddWithValue("@AccountId", accountId),
            cancellationToken);
}
