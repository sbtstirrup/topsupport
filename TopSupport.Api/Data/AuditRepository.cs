using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IAuditRepository, calling usp_Audit_Insert/GetRecent.</summary>
public class AuditRepository : IAuditRepository
{
    private readonly IDbExecutor _db;

    public AuditRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<AuditEntryDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Audit_GetRecent",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    entries.Add(ReadEntry(reader));
                }
            });

        return entries;
    }

    public Task InsertAsync(AuditEntryDto entry, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Audit_Insert",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", entry.Id);
                cmd.Parameters.AddWithValue("@ActorId", entry.ActorId);
                cmd.Parameters.AddWithValue("@Action", entry.Action);
                cmd.Parameters.AddWithValue("@Target", entry.Target);
                cmd.Parameters.AddWithValue("@Category", entry.Category);
            },
            cancellationToken);

    private static AuditEntryDto ReadEntry(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        At = (DateTimeOffset)reader["At"],
        ActorId = (string)reader["ActorId"],
        Action = (string)reader["Action"],
        Target = (string)reader["Target"],
        Category = (string)reader["Category"]
    };
}
