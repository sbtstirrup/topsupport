using System.Net.Http.Json;
using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

/// <summary>Result of a settings save: Error is the demo's error-toast text when the Api rejected it (400).</summary>
public record SettingsSaveOutcome(int Changed, string? Error);

public partial class TopSupportApiClient
{
    // ---------------- Security + site settings (SettingsController) ----------------

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<SecuritySettingsDto>("api/settings/security", cancellationToken) ?? new();

    public async Task<SettingsSaveOutcome> SaveSecuritySettingsAsync(string actorId, SecuritySettingsDto settings, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings/security", new SaveSecuritySettingsRequest { ActorId = actorId, Settings = settings }, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new SettingsSaveOutcome(0, await ReadSettingsErrorAsync(response, cancellationToken));
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SaveSecuritySettingsResult>(cancellationToken);
        return new SettingsSaveOutcome(result?.Changed ?? 0, null);
    }

    public async Task<SiteSettingsDto> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<SiteSettingsDto>("api/settings/site", cancellationToken) ?? new();

    public async Task<SettingsSaveOutcome> SaveSiteSettingsAsync(string actorId, SiteSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings/site", new SaveSiteSettingsRequest { ActorId = actorId, Settings = settings }, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new SettingsSaveOutcome(0, await ReadSettingsErrorAsync(response, cancellationToken));
        }
        response.EnsureSuccessStatusCode();
        return new SettingsSaveOutcome(0, null);
    }

    private sealed record SettingsError(string? Message);

    private static async Task<string> ReadSettingsErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        => (await response.Content.ReadFromJsonAsync<SettingsError>(cancellationToken))?.Message ?? "The settings could not be saved.";

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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileSaveResult>(cancellationToken) ?? new();
    }

    public async Task<ProfileSaveResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/profile/password", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileSaveResult>(cancellationToken) ?? new();
    }
}
