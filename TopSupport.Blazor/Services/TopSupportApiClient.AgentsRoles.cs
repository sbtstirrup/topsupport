using System.Net.Http.Json;
using TopSupport.Models.Admin;
using TopSupport.Models.People;

namespace TopSupport.Blazor.Services;

public partial class TopSupportApiClient
{
    // ---------------- Agents and Teams (AdminAgentsController) ----------------

    /// <summary>Every agent including inactive ones (the lookup endpoint returns active only).</summary>
    public async Task<IReadOnlyList<AgentDto>> GetAdminAgentsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<AgentDto>>("api/admin/agents", cancellationToken) ?? new();

    public async Task<IReadOnlyList<TeamDto>> GetAdminTeamsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<TeamDto>>("api/admin/teams", cancellationToken) ?? new();

    public async Task<ActiveTicketCountsDto> GetAgentTeamActiveTicketCountsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<ActiveTicketCountsDto>("api/admin/agents/active-ticket-counts", cancellationToken) ?? new();

    public async Task<AgentAdminSaveResult> SaveAgentAsync(AgentSaveRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/agents", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new AgentAdminSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentAdminSaveResult>(cancellationToken) ?? new();
    }

    public async Task<AgentPasswordResetResult> ResetAgentPasswordAsync(string agentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/admin/agents/{Uri.EscapeDataString(agentId)}/reset-password", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AgentPasswordResetResult>(cancellationToken))!;
    }

    public async Task<AgentAdminSaveResult> SaveTeamAsync(TeamSaveRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/teams", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return new AgentAdminSaveResult { Errors = await ReadValidationErrorsAsync(response, cancellationToken) };
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentAdminSaveResult>(cancellationToken) ?? new();
    }

    public async Task<TeamDeleteResult> CheckTeamDeleteAsync(string teamId, CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<TeamDeleteResult>($"api/admin/teams/{Uri.EscapeDataString(teamId)}/delete-check", cancellationToken) ?? new();

    public async Task<TeamDeleteResult> DeleteTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/teams/{Uri.EscapeDataString(teamId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TeamDeleteResult>(cancellationToken) ?? new();
    }

    // ---------------- Roles and Permissions (RolePermissionsController) ----------------

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<RolePermissionDto>>("api/admin/permissions", cancellationToken) ?? new();

    public async Task SetRolePermissionAsync(SetRolePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/admin/permissions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
