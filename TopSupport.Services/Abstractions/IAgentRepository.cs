using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.Agents (+ AgentDepartments/TeamMembers), calling
/// usp_Agent_GetById/usp_Agent_GetAllActive.
/// </summary>
public interface IAgentRepository
{
    Task<AgentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
