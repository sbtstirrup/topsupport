using System.Net.Http.Json;
using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

/// <summary>Result of a security settings save: how many settings changed, or the Api's field errors (400).</summary>
public record SecuritySaveOutcome(int Changed, IReadOnlyDictionary<string, string> Errors);

public partial class TopSupportApiClient
{
    // ---------------- Security + site settings (SettingsController) ----------------

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<SecuritySettingsDto>("api/settings/security", cancellationToken);

    public async Task<SecuritySaveOutcome> SaveSecuritySettingsAsync(SecuritySettingsDto settings, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings/security", settings, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new SecuritySaveOutcome(0, await ReadValidationErrorsAsync(response, cancellationToken));
        }
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<SaveSecuritySettingsResult>(cancellationToken)
            ?? throw EmptyBody(response);
        return new SecuritySaveOutcome(result.Changed, new Dictionary<string, string>());
    }

    public async Task<SiteSettingsDto> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<SiteSettingsDto>("api/settings/site", cancellationToken);

    /// <summary>The Api's field errors from a 400 (key "" for one about no single field); empty when saved.</summary>
    public async Task<IReadOnlyDictionary<string, string>> SaveSiteSettingsAsync(SiteSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings/site", settings, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return await ReadValidationErrorsAsync(response, cancellationToken);
        }
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return new Dictionary<string, string>();
    }

    // ---------------- My Profile (ProfileController) ----------------

    /// <summary>The signed-in account's own profile (the Api takes the account from the token).</summary>
    public async Task<ProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/profile", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileDto>(cancellationToken);
    }

    public async Task<ProfileSaveResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/profile", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new ProfileSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return new ProfileSaveResult();
    }

    public async Task<ProfileSaveResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/profile/password", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new ProfileSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return new ProfileSaveResult();
    }
}
