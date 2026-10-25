using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IProfileRepository.</summary>
public class ProfileRepository : IProfileRepository
{
    private readonly DbExecutor _db;

    public ProfileRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<ProfileDto?> GetAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ProfileDto? profile = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Profile_Get",
            cmd => cmd.Parameters.AddWithValue("@AccountId", accountId),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    profile = new ProfileDto
                    {
                        Id = (string)reader["Id"],
                        IsAgent = (bool)reader["IsAgent"],
                        Name = (string)reader["Name"],
                        Email = (string)reader["Email"],
                        Phone = reader["Phone"] as string,
                        JobTitle = reader["JobTitle"] as string,
                        Department = reader["Department"] as string,
                        Office = reader["Office"] as string,
                        Departments = reader["Departments"] as string
                    };
                }
            });

        return profile;
    }

    public Task UpdateAsync(string accountId, string name, string email, string? phone, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Profile_Update",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
            },
            cancellationToken);
}
