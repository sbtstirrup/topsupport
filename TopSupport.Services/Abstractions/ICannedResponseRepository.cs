using TopSupport.Models.Tickets;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.CannedResponses, calling usp_CannedResponse_GetAll/Insert/Update/Delete.
/// </summary>
public interface ICannedResponseRepository
{
    Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CannedResponseDto> InsertAsync(CannedResponseDto response, CancellationToken cancellationToken = default);

    Task UpdateAsync(CannedResponseDto response, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
