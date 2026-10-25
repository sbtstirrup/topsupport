using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// My Profile (PAGES.profile in topsupport-app.js) for any role: the signed-in account's contact
/// details (ACT['pf-save']) and changing their own password (ACT['pf-pw']) against the live
/// security settings.
/// </summary>
public interface IProfileService
{
    Task<ProfileDto?> GetAsync(string accountId, CancellationToken cancellationToken = default);

    Task<ProfileSaveResult> UpdateDetailsAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<ProfileSaveResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _repository;
    private readonly IAccountPasswordService _passwords;
    private readonly ISettingsService _settings;
    private readonly IAuditService _audit;

    public ProfileService(IProfileRepository repository, IAccountPasswordService passwords, ISettingsService settings, IAuditService audit)
    {
        _repository = repository;
        _passwords = passwords;
        _settings = settings;
        _audit = audit;
    }

    public Task<ProfileDto?> GetAsync(string accountId, CancellationToken cancellationToken = default)
        => _repository.GetAsync(accountId, cancellationToken);

    public async Task<ProfileSaveResult> UpdateDetailsAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ProfileSaveResult { Errors = ProfileRules.ValidateDetails(request.Name, request.Email, request.Phone) };
        if (result.Errors.Count > 0) return result;

        var name = request.Name.Trim();
        var phone = request.Phone?.Trim();
        await _repository.UpdateAsync(request.ActorId, name, request.Email.Trim(), string.IsNullOrEmpty(phone) ? null : phone, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Updated profile details", name, AuditCategories.User, cancellationToken);
        return result;
    }

    public async Task<ProfileSaveResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ProfileSaveResult();
        var e = result.Errors;

        if (string.IsNullOrEmpty(request.CurrentPassword)) e["cur"] = "Enter your current password.";
        else if (!await _passwords.VerifyAsync(request.ActorId, request.CurrentPassword, cancellationToken)) e["cur"] = "That is not your current password.";

        // pw !== m.password: only a hash is stored, so "different" means "doesn't verify".
        var differs = !string.IsNullOrEmpty(request.NewPassword)
            && !await _passwords.VerifyAsync(request.ActorId, request.NewPassword, cancellationToken);
        var security = await _settings.GetSecurityAsync(cancellationToken);
        var bad = ProfileRules.PasswordRules(security, request.NewPassword, request.ConfirmPassword, differs).Where(r => !r.Met).ToList();

        if (string.IsNullOrEmpty(request.NewPassword)) e["nw"] = "Enter a new password.";
        else if (bad.Count > 0) e["nw"] = "The new password does not meet: " + string.Join("; ", bad.Select(r => r.Label.ToLowerInvariant())) + ".";

        if (e.Count > 0) return result;

        await _passwords.ChangeOwnAsync(request.ActorId, request.NewPassword, cancellationToken);
        var profile = await _repository.GetAsync(request.ActorId, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Changed own password", profile?.Name ?? request.ActorId, AuditCategories.User, cancellationToken);
        return result;
    }
}
