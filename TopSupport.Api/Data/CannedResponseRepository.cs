using Microsoft.Data.SqlClient;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of ICannedResponseRepository, calling
/// usp_CannedResponse_GetAll/Insert/Update/Delete.
/// </summary>
public class CannedResponseRepository : ICannedResponseRepository
{
    private readonly IDbExecutor _db;

    public CannedResponseRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var responses = new List<CannedResponseDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_CannedResponse_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    responses.Add(ReadResponse(reader));
                }
            });

        return responses;
    }

    public async Task<CannedResponseDto> InsertAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrEmpty(response.Id) ? Guid.NewGuid().ToString("n") : response.Id;

        await _db.ExecuteNonQueryAsync(
            "dbo.usp_CannedResponse_Insert",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Title", response.Title);
                cmd.Parameters.AddWithValue("@Body", response.Body);
                cmd.Parameters.AddWithValue("@SortOrder", response.SortOrder);
            },
            cancellationToken);

        response.Id = id;
        return response;
    }

    public Task UpdateAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_CannedResponse_Update",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", response.Id);
                cmd.Parameters.AddWithValue("@Title", response.Title);
                cmd.Parameters.AddWithValue("@Body", response.Body);
                cmd.Parameters.AddWithValue("@SortOrder", response.SortOrder);
            },
            cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_CannedResponse_Delete",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken);

    private static CannedResponseDto ReadResponse(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        Title = (string)reader["Title"],
        Body = (string)reader["Body"],
        SortOrder = (int)reader["SortOrder"]
    };
}
