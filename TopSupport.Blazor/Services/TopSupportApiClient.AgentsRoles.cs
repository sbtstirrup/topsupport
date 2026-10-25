using System.Net.Http.Json;
using TopSupport.Models.Admin;
using TopSupport.Models.People;

namespace TopSupport.Blazor.Services;

public partial class TopSupportApiClient
{
    // ---------------- Agents and Teams (AdminAgentsController) ----------------

    /// <summary>Every agent including inactive ones (the lookup endpoint returns active only).</summary>
    public async Task<IReadOnlyList<AgentDto>> GetAdminAgentsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<AgentDto>>("api/admin/agents", cancellationToken);

    public async Task<IReadOnlyList<TeamDto>> GetAdminTeamsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<TeamDto>>("api/admin/teams", cancellationToken);

    public async Task<ActiveTicketCountsDto> GetAgentTeamActiveTicketCountsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<ActiveTicketCountsDto>("api/admin/agents/active-ticket-counts", cancellationToken);

    public async Task<AgentAdminSaveResult> SaveAgentAsync(AgentSaveRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/agents", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new AgentAdminSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AgentAdminSaveResult>(cancellationToken) ?? throw EmptyBody(response);
    }

    public async Task<AgentPasswordResetResult> ResetAgentPasswordAsync(string agentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/admin/agents/{Uri.EscapeDataString(agentId)}/reset-password", null, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<AgentPasswordResetResult>(cancellationToken))!;
    }

    public async Task<AgentAdminSaveResult> SaveTeamAsync(TeamSaveRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/teams", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new AgentAdminSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AgentAdminSaveResult>(cancellationToken) ?? throw EmptyBody(response);
    }

    public async Task<TeamDeleteResult> CheckTeamDeleteAsync(string teamId, CancellationToken cancellationToken = default)
        => await GetRequiredAsync<TeamDeleteResult>($"api/admin/teams/{Uri.EscapeDataString(teamId)}/delete-check", cancellationToken);

    public async Task<TeamDeleteResult> DeleteTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/teams/{Uri.EscapeDataString(teamId)}", cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TeamDeleteResult>(cancellationToken) ?? throw EmptyBody(response);
    }

    // ---------------- Roles and Permissions (RolePermissionsController) ----------------

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
        => await GetRequiredAsync<List<RolePermissionDto>>("api/admin/permissions", cancellationToken);

    public async Task SetRolePermissionAsync(SetRolePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/admin/permissions", request, cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }
}
