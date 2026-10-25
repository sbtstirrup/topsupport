using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IOrgRepository (usp_Org_* procs in 08_Admin_UsersOrgs.sql).</summary>
public class OrgRepository : IOrgRepository
{
    private readonly DbExecutor _db;

    public OrgRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrgListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var orgs = new List<OrgListItemDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Org_GetList",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var org = new OrgListItemDto
                    {
                        MemberCount = (int)reader["MemberCount"],
                        OpenTicketCount = (int)reader["OpenTicketCount"]
                    };
                    Fill(org, reader);
                    orgs.Add(org);
                }
            });

        return orgs;
    }

    public async Task<OrgDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        OrgDto? org = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Org_GetById",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    org = new OrgDto();
                    Fill(org, reader);
                }
            });

        return org;
    }

    public async Task<IReadOnlyList<string>> GetOpenTicketIdsAsync(string orgId, CancellationToken cancellationToken = default)
    {
        var ids = new List<string>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Org_GetOpenTicketIds",
            cmd => cmd.Parameters.AddWithValue("@OrgId", orgId),
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    ids.Add((string)reader["Id"]);
                }
            });

        return ids;
    }

    public Task InsertAsync(OrgDto org, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync("dbo.usp_Org_Insert", cmd => AddOrgParameters(cmd, org), cancellationToken);

    public Task UpdateAsync(OrgDto org, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync("dbo.usp_Org_Update", cmd => AddOrgParameters(cmd, org), cancellationToken);

    private static void AddOrgParameters(SqlCommand cmd, OrgDto org)
    {
        cmd.Parameters.AddWithValue("@Id", org.Id);
        cmd.Parameters.AddWithValue("@Name", org.Name);
        cmd.Parameters.AddWithValue("@Address", org.Address);
        cmd.Parameters.AddWithValue("@Phone", org.Phone);
        cmd.Parameters.AddWithValue("@Note", (object?)org.Note ?? DBNull.Value);
    }

    private static void Fill(OrgDto org, SqlDataReader reader)
    {
        org.Id = (string)reader["Id"];
        org.Name = (string)reader["Name"];
        org.Address = (string)reader["Address"];
        org.Phone = (string)reader["Phone"];
        org.Note = reader["Note"] as string;
    }
}
