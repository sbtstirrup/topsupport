using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IAppSettingsRepository.</summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly IDbExecutor _db;

    public AppSettingsRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_AppSettings_GetByPrefix",
            cmd => cmd.Parameters.AddWithValue("@Prefix", prefix),
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    values[((string)reader["Key"])[prefix.Length..]] = (string)reader["Value"];
                }
            });

        return values;
    }

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_AppSetting_Set",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@Value", value);
            },
            cancellationToken);
}
