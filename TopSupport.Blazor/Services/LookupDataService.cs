using TopSupport.Models.People;

namespace TopSupport.Blazor.Services;

/// <summary>
/// Caches the small, rarely-changing lookup pools (agents/teams/users) per-circuit so pages
/// like PersonCombo/queue filters/assignment modals don't each re-fetch the same data. Scoped
/// (one instance per Blazor Server circuit). Assets are per-user (no "all assets" endpoint --
/// see LookupController), so they're cached per userId rather than as one pool.
///
/// Subscribes to SessionState.Changed and clears its cache on role switch, since a different
/// signed-in account could in principle see different/updated lookup data (and simply keeps
/// the demo's "nothing goes stale across a role swap" expectation).
/// </summary>
public class LookupDataService : IDisposable
{
    private readonly TopSupportApiClient _api;
    private readonly SessionState _session;

    // Every user including deleted ones (so FindUser can still name a deleted requester on an old
    // ticket); GetUsersAsync hands out only the non-deleted ones, for pickers.
    private IReadOnlyList<UserDto>? _users;
    private IReadOnlyList<UserDto>? _activeUsers;
    private IReadOnlyList<AgentDto>? _agents;
    private IReadOnlyList<TeamDto>? _teams;
    private readonly Dictionary<string, IReadOnlyList<AssetDto>> _assetsByUser = new();

    private Task<IReadOnlyList<UserDto>>? _usersTask;
    private Task<IReadOnlyList<AgentDto>>? _agentsTask;
    private Task<IReadOnlyList<TeamDto>>? _teamsTask;

    public LookupDataService(TopSupportApiClient api, SessionState session)
    {
        _api = api;
        _session = session;
        _session.Changed += ClearCache;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (forceRefresh) { _users = null; _activeUsers = null; _usersTask = null; }
        if (_activeUsers is not null) return _activeUsers;
        _usersTask ??= FetchUsersAsync(cancellationToken);
        _users = await _usersTask;
        return _activeUsers = _users.Where(u => !u.Deleted).ToList();
    }

    public async Task<IReadOnlyList<AgentDto>> GetAgentsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (forceRefresh) { _agents = null; _agentsTask = null; }
        if (_agents is not null) return _agents;
        _agentsTask ??= FetchAgentsAsync(cancellationToken);
        return _agents = await _agentsTask;
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (forceRefresh) { _teams = null; _teamsTask = null; }
        if (_teams is not null) return _teams;
        _teamsTask ??= FetchTeamsAsync(cancellationToken);
        return _teams = await _teamsTask;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsByUserAsync(string userId, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && _assetsByUser.TryGetValue(userId, out var cached)) return cached;
        var assets = await _api.GetAssetsByUserAsync(userId, cancellationToken);
        _assetsByUser[userId] = assets;
        return assets;
    }

    /// <summary>Convenience lookups over the cached pools -- callers should GetXAsync first to
    /// ensure the pool is populated; these return null if the id isn't found (or not yet loaded).</summary>
    public UserDto? FindUser(string? id) => id is null ? null : _users?.FirstOrDefault(u => u.Id == id);
    public AgentDto? FindAgent(string? id) => id is null ? null : _agents?.FirstOrDefault(a => a.Id == id);
    public TeamDto? FindTeam(string? id) => id is null ? null : _teams?.FirstOrDefault(t => t.Id == id);

    private async Task<IReadOnlyList<UserDto>> FetchUsersAsync(CancellationToken ct) => await _api.GetAllUsersIncludingDeletedAsync(ct);
    private async Task<IReadOnlyList<AgentDto>> FetchAgentsAsync(CancellationToken ct) => await _api.GetAgentsAsync(ct);
    private async Task<IReadOnlyList<TeamDto>> FetchTeamsAsync(CancellationToken ct) => await _api.GetTeamsAsync(ct);

    private void ClearCache()
    {
        _users = null;
        _activeUsers = null;
        _agents = null;
        _teams = null;
        _usersTask = null;
        _agentsTask = null;
        _teamsTask = null;
        _assetsByUser.Clear();
    }

    public void Dispose() => _session.Changed -= ClearCache;
}
