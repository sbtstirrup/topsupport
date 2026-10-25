using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IDashboardRepository, calling usp_Dashboard_GetTicketClosedAt.</summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly IDbExecutor _db;

    public DashboardRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetTicketClosedAtAsync(CancellationToken cancellationToken = default)
    {
        var closedAt = new Dictionary<string, DateTimeOffset>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Dashboard_GetTicketClosedAt",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    closedAt[(string)reader["TicketId"]] = (DateTimeOffset)reader["ClosedAt"];
                }
            });

        return closedAt;
    }
}
