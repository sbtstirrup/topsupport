using System.Net;
using System.Net.Http.Json;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;

namespace TopSupport.Blazor.Services;

/// <summary>Outcome of an admin save: the saved value, or the field errors from a 400 (demo errs keys).</summary>
public record UserOrgSaveResult<T>(T? Value, IReadOnlyDictionary<string, string> Errors)
{
    public bool Ok => Errors.Count == 0;
}

public partial class TopSupportApiClient
{
    // ---------------- Admin: Users (UsersController) ----------------

    public async Task<IReadOnlyList<UserListItemDto>> GetUserListAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<UserListItemDto>>("api/users", cancellationToken) ?? new();

    /// <summary>Every user including deleted ones (LookupDataService uses this to name people on old tickets).</summary>
    public async Task<IReadOnlyList<UserDto>> GetAllUsersIncludingDeletedAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<UserDto>>("api/users/all", cancellationToken) ?? new();

    public async Task<UserDto?> GetUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/users/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    /// <summary>Creates (id empty) or updates a user.</summary>
    public async Task<UserOrgSaveResult<SaveUserResult>> SaveUserAsync(string? id, SaveUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = string.IsNullOrEmpty(id)
            ? await _httpClient.PostAsJsonAsync("api/users", request, cancellationToken)
            : await _httpClient.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return await ReadSaveResultAsync<SaveUserResult>(response, cancellationToken);
    }

    public async Task<UserDto> ToggleUserLockAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/users/{Uri.EscapeDataString(id)}/toggle-lock", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken))!;
    }

    public async Task<TemporaryPasswordDto> ResetUserPasswordAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/users/{Uri.EscapeDataString(id)}/reset-password", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TemporaryPasswordDto>(cancellationToken))!;
    }

    public async Task DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/users/{Uri.EscapeDataString(id)}/delete", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ---------------- Admin: Organisations (OrgsController) ----------------

    public async Task<IReadOnlyList<OrgListItemDto>> GetOrgListAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<OrgListItemDto>>("api/orgs", cancellationToken) ?? new();

    public async Task<OrgDto?> GetOrgAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/orgs/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrgDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketDto>> GetOrgOpenTicketsAsync(string id, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TicketDto>>($"api/orgs/{Uri.EscapeDataString(id)}/open-tickets", cancellationToken) ?? new();

    /// <summary>Creates (id empty) or updates an organisation.</summary>
    public async Task<UserOrgSaveResult<OrgDto>> SaveOrgAsync(string? id, SaveOrgRequest request, CancellationToken cancellationToken = default)
    {
        var response = string.IsNullOrEmpty(id)
            ? await _httpClient.PostAsJsonAsync("api/orgs", request, cancellationToken)
            : await _httpClient.PutAsJsonAsync($"api/orgs/{Uri.EscapeDataString(id)}", request, cancellationToken);
        return await ReadSaveResultAsync<OrgDto>(response, cancellationToken);
    }

    private static async Task<UserOrgSaveResult<T>> ReadSaveResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new UserOrgSaveResult<T>(default, await ReadValidationErrorsAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return new UserOrgSaveResult<T>(await response.Content.ReadFromJsonAsync<T>(cancellationToken), new Dictionary<string, string>());
    }
}
