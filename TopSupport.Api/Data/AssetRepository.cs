using Microsoft.Data.SqlClient;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IAssetRepository, calling usp_Asset_GetByTag/usp_Asset_GetByUserId.</summary>
public class AssetRepository : IAssetRepository
{
    private readonly IDbExecutor _db;

    public AssetRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<AssetDto?> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        AssetDto? asset = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Asset_GetByTag",
            cmd => cmd.Parameters.AddWithValue("@Tag", tag),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    asset = ReadAsset(reader);
                }
            });

        return asset;
    }

    public async Task<IReadOnlyList<AssetDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var assets = new List<AssetDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Asset_GetByUserId",
            cmd => cmd.Parameters.AddWithValue("@UserId", userId),
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    assets.Add(ReadAsset(reader));
                }
            });

        return assets;
    }

    private static AssetDto ReadAsset(SqlDataReader reader) => new()
    {
        Tag = (string)reader["Tag"],
        Type = (string)reader["Type"],
        Model = (string)reader["Model"],
        UserId = reader["UserId"] as string
    };
}
