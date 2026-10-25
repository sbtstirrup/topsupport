using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IRolePermissionRepository, calling usp_RolePermission_GetAll/Set.</summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly DbExecutor _db;

    public RolePermissionRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var permissions = new List<RolePermissionDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_RolePermission_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    permissions.Add(new RolePermissionDto
                    {
                        Department = (string)reader["Department"],
                        Role = (string)reader["Role"],
                        Permission = (string)reader["Permission"],
                        Allowed = (bool)reader["Allowed"]
                    });
                }
            });

        return permissions;
    }

    public Task SetAsync(string department, string role, string permission, bool allowed, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_RolePermission_Set",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Department", department);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@Permission", permission);
                cmd.Parameters.AddWithValue("@Allowed", allowed);
            },
            cancellationToken);
}
