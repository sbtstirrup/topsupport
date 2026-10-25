using Microsoft.Data.SqlClient;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IUserRepository, calling usp_User_GetById/usp_User_GetAllActive.</summary>
public class UserRepository : IUserRepository
{
    private readonly IDbExecutor _db;

    public UserRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        UserDto? user = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_User_GetById",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    user = ReadUser(reader);
                }
            });

        return user;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_User_GetAllActive",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    users.Add(ReadUser(reader));
                }
            });

        return users;
    }

    private static UserDto ReadUser(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        Name = (string)reader["Name"],
        Username = (string)reader["Username"],
        Email = (string)reader["Email"],
        Phone = reader["Phone"] as string,
        OrgId = (string)reader["OrgId"],
        Office = reader["Office"] as string,
        Department = (string)reader["Department"],
        JobTitle = reader["JobTitle"] as string,
        Locked = (bool)reader["Locked"],
        Deleted = (bool)reader["Deleted"]
    };
}
