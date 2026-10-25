using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// My Profile (PAGES.profile in topsupport-app.js) for any role: the signed-in account's contact
/// details (ACT['pf-save']) and changing their own password (ACT['pf-pw']) against the live
/// security settings.
/// </summary>
public class ProfileService
{
    private readonly IProfileRepository _repository;
    private readonly AccountPasswordService _passwords;
    private readonly SettingsService _settings;
    private readonly AuditService _audit;

    public ProfileService(IProfileRepository repository, AccountPasswordService passwords, SettingsService settings, AuditService audit)
    {
        _repository = repository;
        _passwords = passwords;
        _settings = settings;
        _audit = audit;
    }

    public Task<ProfileDto?> GetAsync(string accountId, CancellationToken cancellationToken = default)
        => _repository.GetAsync(accountId, cancellationToken);

    public async Task UpdateDetailsAsync(string accountId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var errors = ProfileRules.ValidateDetails(request.Name, request.Email, request.Phone);
        if (errors.Count > 0) throw new ValidationException(errors);

        var name = request.Name.Trim();
        var phone = request.Phone?.Trim();
        await _repository.UpdateAsync(accountId, name, request.Email.Trim(), string.IsNullOrEmpty(phone) ? null : phone, cancellationToken);
        await _audit.LogAsync(accountId, "Updated profile details", name, AuditCategories.User, cancellationToken);
    }

    public async Task ChangePasswordAsync(string accountId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(request.CurrentPassword)) errors["cur"] = "Enter your current password.";
        else if (!await _passwords.VerifyAsync(accountId, request.CurrentPassword, cancellationToken)) errors["cur"] = "That is not your current password.";

        // pw !== m.password: only a hash is stored, so "different" means "doesn't verify".
        var differs = !string.IsNullOrEmpty(request.NewPassword)
            && !await _passwords.VerifyAsync(accountId, request.NewPassword, cancellationToken);
        var security = await _settings.GetSecurityAsync(cancellationToken);
        var bad = ProfileRules.PasswordRules(security, request.NewPassword, request.ConfirmPassword, differs).Where(r => !r.Met).ToList();

        if (string.IsNullOrEmpty(request.NewPassword)) errors["nw"] = "Enter a new password.";
        else if (bad.Count > 0) errors["nw"] = "The new password does not meet: " + string.Join("; ", bad.Select(r => r.Label.ToLowerInvariant())) + ".";

        if (errors.Count > 0) throw new ValidationException(errors);

        await _passwords.ChangeOwnAsync(accountId, request.NewPassword, cancellationToken);
        var profile = await _repository.GetAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account '{accountId}' changed its password but has no profile.");
        await _audit.LogAsync(accountId, "Changed own password", profile.Name, AuditCategories.User, cancellationToken);
    }
}
