using TopSupport.Models.Admin;
using TopSupport.Models.People;

namespace TopSupport.Services.Abstractions;

/// <summary>Whether an email/username is already used by another account (validateUser's duplicate checks).</summary>
public record AccountIdentityTaken(bool EmailTaken, bool UsernameTaken);

/// <summary>
/// Data-access contract for the admin Users screens, calling the usp_AdminUser_*/usp_User_*
/// procs in TopSupport.Database/Scripts/08_Admin_UsersOrgs.sql. Single-user reads stay on
/// IUserRepository.GetByIdAsync.
/// </summary>
public interface IUserAdminRepository
{
    /// <summary>Non-deleted users with their active-ticket counts.</summary>
    Task<IReadOnlyList<UserListItemDto>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>Every user including deleted ones (for naming people on historical tickets).</summary>
    Task<IReadOnlyList<UserDto>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    Task<AccountIdentityTaken> CheckIdentityTakenAsync(string excludeId, string email, string username, CancellationToken cancellationToken = default);

    Task InsertAsync(UserDto user, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserDto user, CancellationToken cancellationToken = default);

    Task SetLockedAsync(string id, bool locked, CancellationToken cancellationToken = default);

    /// <summary>Marks the user deleted and locked, and unassigns their equipment.</summary>
    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);
}
