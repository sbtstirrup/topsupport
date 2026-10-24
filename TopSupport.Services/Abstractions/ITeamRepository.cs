using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.Teams (+ TeamMembers), calling usp_Team_GetById/usp_Team_GetAll.
/// </summary>
public interface ITeamRepository
{
    Task<TeamDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
