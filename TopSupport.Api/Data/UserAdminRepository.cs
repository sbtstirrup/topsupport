using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IUserAdminRepository (procs in 08_Admin_UsersOrgs.sql).</summary>
public class UserAdminRepository : IUserAdminRepository
{
    private readonly DbExecutor _db;

    public UserAdminRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserListItemDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminUser_GetList",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var user = new UserListItemDto { OpenTicketCount = (int)reader["OpenTicketCount"] };
                    Fill(user, reader);
                    users.Add(user);
                }
            });

        return users;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_User_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var user = new UserDto();
                    Fill(user, reader);
                    users.Add(user);
                }
            });

        return users;
    }

    public async Task<AccountIdentityTaken> CheckIdentityTakenAsync(string excludeId, string email, string username, CancellationToken cancellationToken = default)
    {
        var result = new AccountIdentityTaken(false, false);

        await _db.ExecuteReaderAsync(
            "dbo.usp_Account_CheckIdentityTaken",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Username", username);
            },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    result = new AccountIdentityTaken((bool)reader["EmailTaken"], (bool)reader["UsernameTaken"]);
                }
            });

        return result;
    }

    public Task InsertAsync(UserDto user, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync("dbo.usp_User_Insert", cmd => AddUserParameters(cmd, user), cancellationToken);

    public Task UpdateAsync(UserDto user, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync("dbo.usp_User_Update", cmd => AddUserParameters(cmd, user), cancellationToken);

    public Task SetLockedAsync(string id, bool locked, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_User_SetLocked",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Locked", locked);
            },
            cancellationToken);

    public Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_User_SoftDelete",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken);

    private static void AddUserParameters(SqlCommand cmd, UserDto user)
    {
        cmd.Parameters.AddWithValue("@Id", user.Id);
        cmd.Parameters.AddWithValue("@Name", user.Name);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OrgId", user.OrgId);
        cmd.Parameters.AddWithValue("@Office", (object?)user.Office ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Department", user.Department);
        cmd.Parameters.AddWithValue("@JobTitle", (object?)user.JobTitle ?? DBNull.Value);
    }

    private static void Fill(UserDto user, SqlDataReader reader)
    {
        user.Id = (string)reader["Id"];
        user.Name = (string)reader["Name"];
        user.Username = (string)reader["Username"];
        user.Email = (string)reader["Email"];
        user.Phone = reader["Phone"] as string;
        user.OrgId = (string)reader["OrgId"];
        user.Office = reader["Office"] as string;
        user.Department = (string)reader["Department"];
        user.JobTitle = reader["JobTitle"] as string;
        user.Locked = (bool)reader["Locked"];
        user.Deleted = (bool)reader["Deleted"];
    }
}
