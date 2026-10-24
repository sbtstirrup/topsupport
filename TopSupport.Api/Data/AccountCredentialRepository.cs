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

    public async Task<AccountCredential?> GetAsync(string accountId, CancellationToken cancellationToken = default)
    {
        AccountCredential? credential = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_GetPasswordHash",
            cmd => cmd.Parameters.AddWithValue("@AccountId", accountId),
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    credential = new AccountCredential(
                        (string)reader["AccountType"],
                        reader["PasswordHash"] as string,
                        reader["AgentRole"] as string);
                }
            });

        return credential;
    }

    public Task SetPasswordHashAsync(string accountId, string passwordHash, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Account_SetPasswordHash",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            },
            cancellationToken);
}
