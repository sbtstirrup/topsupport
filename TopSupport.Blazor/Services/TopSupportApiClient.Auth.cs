using System.Net;
using System.Net.Http.Json;
using TopSupport.Services.Auth;

namespace TopSupport.Blazor.Services;

/// <summary>POST api/auth/sign-in's success body (AuthController.SignInResponse).</summary>
public record SignInResponse(string Token, DateTimeOffset ExpiresAt, string AccountId, string Role, string DisplayName);

public partial class TopSupportApiClient
{
    // ---------------- Auth (AuthController) ----------------

    private record SignInError(string Error);

    /// <summary>The signed-in account and its token, or the sign-in page's error message.</summary>
    public async Task<(SignInResponse? Account, string? Error)> SignInAsync(string? username, string? password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/sign-in", new { username, password }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<SignInError>(cancellationToken);
            return (null, error?.Error ?? "Sign-in failed.");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInResponse>(cancellationToken), null);
    }

    /// <summary>The test-account switcher's list; empty when the Api has the switcher turned off (404).</summary>
    public async Task<IReadOnlyList<TestAccountItem>> GetTestAccountsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/auth/test-accounts", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return Array.Empty<TestAccountItem>();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TestAccountItem>>(cancellationToken) ?? throw EmptyBody(response);
    }

    /// <summary>Test-account switcher: a token for any account without its password (Development only).</summary>
    public async Task<(SignInResponse? Account, string? Error)> TestSignInAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/test-sign-in", new { accountId }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<SignInError>(cancellationToken);
            return (null, error?.Error ?? "That account can't sign in.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound) return (null, "The test-account switcher is turned off in the Api.");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInResponse>(cancellationToken), null);
    }

    /// <summary>Audits the sign-out as the signed-in account.</summary>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/auth/sign-out", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
