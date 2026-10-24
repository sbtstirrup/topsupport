using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>Thrown when a settings save is rejected (the demo's error toast). Mapped to 400 by the Api.</summary>
public class SettingsValidationException : Exception
{
    public SettingsValidationException(string message) : base(message)
    {
    }
}

/// <summary>
/// The demo's DB.security and DB.site, stored in dbo.AppSettings under "security." / "site." keys.
/// Security settings also drive the password rules on My Profile (pwRules in topsupport-app.js).
/// </summary>
public interface ISettingsService
{
    Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken cancellationToken = default);

    /// <summary>Port of ACT['sec-save']: validates, stores, audits each changed setting and returns how many changed.</summary>
    Task<int> SaveSecurityAsync(string actorId, SecuritySettingsDto settings, CancellationToken cancellationToken = default);

    Task<SiteSettingsDto> GetSiteAsync(CancellationToken cancellationToken = default);

    /// <summary>Port of ACT['site-save'].</summary>
    Task SaveSiteAsync(string actorId, SiteSettingsDto settings, CancellationToken cancellationToken = default);
}

public class SettingsService : ISettingsService
{
    private const string SecurityPrefix = "security.";
    private const string SitePrefix = "site.";

    private readonly IAppSettingsRepository _repository;
    private readonly IAuditService _audit;

    public SettingsService(IAppSettingsRepository repository, IAuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken cancellationToken = default)
        => SecuritySettingsDto.FromKeyValues(await _repository.GetByPrefixAsync(SecurityPrefix, cancellationToken));

    public async Task<int> SaveSecurityAsync(string actorId, SecuritySettingsDto settings, CancellationToken cancellationToken = default)
    {
        // The demo clamps the three number inputs to >= 0 before checking the minimum length.
        settings.MinLength = Math.Max(0, settings.MinLength);
        settings.ExpiryDays = Math.Max(0, settings.ExpiryDays);
        settings.History = Math.Max(0, settings.History);
        if (settings.MinLength < 8) throw new SettingsValidationException("Minimum length must be at least 8.");

        var current = (await GetSecurityAsync(cancellationToken)).ToKeyValues().ToDictionary(kv => kv.Key, kv => kv.Value);
        var changed = settings.ToKeyValues().Where(kv => current[kv.Key] != kv.Value).ToList();

        foreach (var (key, value) in changed)
        {
            await _repository.SetAsync(SecurityPrefix + key, value, cancellationToken);
            await _audit.LogAsync(actorId, $"Updated security setting \"{key}\" to {value}", "Security", AuditCategories.Permission, cancellationToken);
        }

        return changed.Count;
    }

    public async Task<SiteSettingsDto> GetSiteAsync(CancellationToken cancellationToken = default)
    {
        var values = await _repository.GetByPrefixAsync(SitePrefix, cancellationToken);
        var site = new SiteSettingsDto();
        if (values.TryGetValue("landingTitle", out var v)) site.LandingTitle = v;
        if (values.TryGetValue("landingBody", out v)) site.LandingBody = v;
        if (values.TryGetValue("signinTitle", out v)) site.SigninTitle = v;
        if (values.TryGetValue("signinBody", out v)) site.SigninBody = v;
        return site;
    }

    public async Task SaveSiteAsync(string actorId, SiteSettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.LandingTitle) || string.IsNullOrWhiteSpace(settings.SigninTitle))
        {
            throw new SettingsValidationException("Headings cannot be empty.");
        }

        // Stored as typed, like DB.site = Object.assign({}, F) in the demo (no trimming).
        await _repository.SetAsync(SitePrefix + "landingTitle", settings.LandingTitle, cancellationToken);
        await _repository.SetAsync(SitePrefix + "landingBody", settings.LandingBody ?? string.Empty, cancellationToken);
        await _repository.SetAsync(SitePrefix + "signinTitle", settings.SigninTitle, cancellationToken);
        await _repository.SetAsync(SitePrefix + "signinBody", settings.SigninBody ?? string.Empty, cancellationToken);
        await _audit.LogAsync(actorId, "Edited site pages", "Landing and sign-in", AuditCategories.Permission, cancellationToken);
    }
}
