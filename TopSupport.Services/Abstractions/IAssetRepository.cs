using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.Assets, calling usp_Asset_GetByTag/usp_Asset_GetByUserId.
/// </summary>
public interface IAssetRepository
{
    Task<AssetDto?> GetByTagAsync(string tag, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
