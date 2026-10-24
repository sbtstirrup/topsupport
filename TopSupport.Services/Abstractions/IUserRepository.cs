using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.Users, calling usp_User_GetById/usp_User_GetAllActive
/// (TopSupport.Database/Scripts/05_StoredProcedures_Extensions.sql).
/// </summary>
public interface IUserRepository
{
    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
