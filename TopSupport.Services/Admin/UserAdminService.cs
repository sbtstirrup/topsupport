using System.Text.RegularExpressions;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Admin Users screens: port of PAGES.users/PAGES.user, ACT['user-save'], ACT['user-lock'],
/// ACT['user-reset'] and ACT['user-del'] in topsupport-app.js. Every mutation audits with the
/// demo's wording. Throws <see cref="UserOrgValidationException"/> for form errors and
/// <see cref="KeyNotFoundException"/> when the user doesn't exist (or is deleted).
/// </summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItemDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates the user with a generated temporary password, returned once in the result.</summary>
    Task<SaveUserResult> CreateAsync(SaveUserRequest request, CancellationToken cancellationToken = default);

    Task<SaveUserResult> UpdateAsync(string id, SaveUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Flips Locked and returns the updated user.</summary>
    Task<UserDto> ToggleLockAsync(string id, string actorId, CancellationToken cancellationToken = default);

    /// <summary>Sets (and unlocks with) a new temporary password, returned once.</summary>
    Task<TemporaryPasswordDto> ResetPasswordAsync(string id, string actorId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the user (locked too) and unassigns their equipment.</summary>
    Task DeleteAsync(string id, string actorId, CancellationToken cancellationToken = default);
}

public partial class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly IUserAdminRepository _repository;
    private readonly IOrgRepository _orgs;
    private readonly IAccountPasswordService _passwords;
    private readonly IAuditService _audit;

    public UserAdminService(IUserRepository users, IUserAdminRepository repository, IOrgRepository orgs, IAccountPasswordService passwords, IAuditService audit)
    {
        _users = users;
        _repository = repository;
        _orgs = orgs;
        _passwords = passwords;
        _audit = audit;
    }

    public Task<IReadOnlyList<UserListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
        => _repository.GetListAsync(cancellationToken);

    public Task<IReadOnlyList<UserDto>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllIncludingDeletedAsync(cancellationToken);

    public async Task<SaveUserResult> CreateAsync(SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(string.Empty, request, cancellationToken);

        var user = ToUser("u" + Guid.NewGuid().ToString("n")[..12], request);
        await _repository.InsertAsync(user, cancellationToken);

        var password = _passwords.GenerateTemporary();
        await _passwords.SetAsync(user.Id, password, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Created user", user.Name, AuditCategories.User, cancellationToken);

        return new SaveUserResult
        {
            User = user,
            TemporaryPassword = new TemporaryPasswordDto { Name = user.Name, Username = user.Username, Password = password }
        };
    }

    public async Task<SaveUserResult> UpdateAsync(string id, SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await GetActiveUserAsync(id, cancellationToken);
        await ValidateAsync(id, request, cancellationToken);

        var user = ToUser(id, request);
        user.Locked = existing.Locked;
        await _repository.UpdateAsync(user, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Edited user", user.Name, AuditCategories.User, cancellationToken);

        return new SaveUserResult { User = user };
    }

    public async Task<UserDto> ToggleLockAsync(string id, string actorId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(id, cancellationToken);
        user.Locked = !user.Locked;
        await _repository.SetLockedAsync(id, user.Locked, cancellationToken);
        await _audit.LogAsync(actorId, (user.Locked ? "Locked" : "Unlocked") + " user account", user.Name, AuditCategories.User, cancellationToken);
        return user;
    }

    public async Task<TemporaryPasswordDto> ResetPasswordAsync(string id, string actorId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(id, cancellationToken);
        var password = _passwords.GenerateTemporary();
        await _passwords.SetAsync(id, password, cancellationToken);
        await _audit.LogAsync(actorId, "Reset password", user.Name, AuditCategories.User, cancellationToken);
        return new TemporaryPasswordDto { Name = user.Name, Username = user.Username, Password = password };
    }

    public async Task DeleteAsync(string id, string actorId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(id, cancellationToken);
        await _repository.SoftDeleteAsync(id, cancellationToken);
        await _audit.LogAsync(actorId, "Deleted user", user.Name, AuditCategories.User, cancellationToken);
    }

    private async Task<UserDto> GetActiveUserAsync(string id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null || user.Deleted) throw new KeyNotFoundException($"User {id} not found.");
        return user;
    }

    /// <summary>Port of validateUser(F, false) with the demo's exact messages.</summary>
    private async Task ValidateAsync(string id, SaveUserRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();
        var email = request.Email.Trim();
        var username = request.Username.Trim();
        var taken = await _repository.CheckIdentityTakenAsync(id, email, username, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = "Enter a name.";
        else if (FieldLimits.TooLong(request.Name, FieldLimits.PersonName) is { } nameErr) errors["name"] = nameErr;

        // Length first: the column is 256 and the regex shouldn't scan an unbounded string.
        if (FieldLimits.TooLong(email, FieldLimits.Email) is { } emailErr) errors["email"] = emailErr;
        else if (!EmailPattern().IsMatch(email)) errors["email"] = "Enter a valid email address.";
        else if (taken.EmailTaken) errors["email"] = "Someone already uses this email.";

        if (!UsernamePattern().IsMatch(username)) errors["username"] = "Use 3 to 20 lower-case letters.";
        else if (taken.UsernameTaken) errors["username"] = "This username is taken.";

        if (FieldLimits.TooLong(request.Phone, FieldLimits.Phone) is { } phoneErr) errors["phone"] = phoneErr;
        else if (!FieldLimits.IsValidPhone(request.Phone)) errors["phone"] = "Use digits, spaces and + only.";

        // The demo's select has no placeholder, so this only catches a tampered/stale org id.
        if (await _orgs.GetByIdAsync(request.OrgId, cancellationToken) is null) errors["org"] = "Choose an organisation.";

        if (string.IsNullOrWhiteSpace(request.Office)) errors["office"] = "Enter the office or desk.";
        else if (FieldLimits.TooLong(request.Office, FieldLimits.Office) is { } officeErr) errors["office"] = officeErr;
        if (!UserDepartments.All.Contains(request.Department)) errors["department"] = "Choose a department.";
        if (string.IsNullOrWhiteSpace(request.JobTitle)) errors["jobTitle"] = "Enter a job title.";
        else if (FieldLimits.TooLong(request.JobTitle, FieldLimits.JobTitle) is { } jobErr) errors["jobTitle"] = jobErr;

        if (errors.Count > 0) throw new UserOrgValidationException(errors);
    }

    private static UserDto ToUser(string id, SaveUserRequest request) => new()
    {
        Id = id,
        Name = request.Name.Trim(),
        Email = request.Email.Trim(),
        Username = request.Username.Trim(),
        Phone = request.Phone.Trim(),
        OrgId = request.OrgId,
        Office = request.Office.Trim(),
        Department = request.Department,
        JobTitle = request.JobTitle.Trim()
    };

    // Same patterns as validateUser(): /^[^@\s]+@[^@\s]+\.[^@\s]+$/ and /^[a-z]{3,20}$/.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex("^[a-z]{3,20}$")]
    private static partial Regex UsernamePattern();
}
