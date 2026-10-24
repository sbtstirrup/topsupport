using TopSupport.Models.Admin;

namespace TopSupport.Services.Abstractions;

/// <summary>Data-access contract for My Profile (a Users or Agents row), calling usp_Profile_Get/Update.</summary>
public interface IProfileRepository
{
    Task<ProfileDto?> GetAsync(string accountId, CancellationToken cancellationToken = default);

    Task UpdateAsync(string accountId, string name, string email, string? phone, CancellationToken cancellationToken = default);
}
