using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Tests.Fakes;

// In-memory repositories. Each implements only what the services under test call and throws
// for anything else, so a test can't quietly pass through an unexpected path.

public sealed class FakeTicketRepository : ITicketRepository
{
    public Dictionary<string, TicketDto> Tickets { get; } = new();
    public List<(string TicketId, string Status)> StatusUpdates { get; } = new();

    public Task<TicketDto?> GetByIdAsync(string ticketId, CancellationToken cancellationToken = default)
        => Task.FromResult(Tickets.GetValueOrDefault(ticketId));

    public Task UpdateStatusAsync(string ticketId, string status, DateTimeOffset updated, CancellationToken cancellationToken = default)
    {
        StatusUpdates.Add((ticketId, status));
        return Task.CompletedTask;
    }

    public Task<TicketTimelineEntryDto> AppendTimelineEntryAsync(TicketTimelineEntryDto entry, CancellationToken cancellationToken = default)
        => Task.FromResult(entry);

    public Task<IReadOnlyList<TicketDto>> GetByRequesterOrAffectedAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<TicketDto> CreateAsync(TicketDto ticket, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> GetNextNumberAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UpdatePriorityAsync(string ticketId, string priority, DateTimeOffset updated, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UpdateTopicAsync(string ticketId, string topic, string department, DateTimeOffset updated, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AssignAsync(string ticketId, string? assignedAgentId, string? assignedTeamId, DateTimeOffset updated, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> ClaimIfUnchangedAsync(string ticketId, string agentId, string? assignedTeamId, string? expectedAgentId, DateTimeOffset updated, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<TicketTaskDto> InsertTaskAsync(TicketTaskDto task, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SetTaskDoneAsync(string taskId, bool done, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<TicketDto>> GetActiveForQueueAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<TicketDto>> SearchForQueueAsync(string? status, string? priority, string? topic, string? department, string? assignedAgentId, string? assignedTeamId, string? searchText, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public sealed class FakeUserRepository : IUserRepository
{
    public List<UserDto> Users { get; } = new();

    public Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<UserDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserDto>>(Users.Where(u => !u.Deleted).ToList());
}

public sealed class FakeAgentRepository : IAgentRepository
{
    public List<AgentDto> Agents { get; } = new();

    public Task<AgentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Agents.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<AgentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AgentDto>>(Agents.Where(a => a.Active).ToList());
}

public sealed class FakeTeamRepository : ITeamRepository
{
    public Task<TeamDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

/// <summary>Nobody has assets: the general topic used in the tests has no asset fields.</summary>
public sealed class FakeAssetRepository : IAssetRepository
{
    public Task<AssetDto?> GetByTagAsync(string tag, CancellationToken cancellationToken = default) => Task.FromResult<AssetDto?>(null);

    public Task<IReadOnlyList<AssetDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AssetDto>>(Array.Empty<AssetDto>());
}

public sealed class FakeAuditRepository : IAuditRepository
{
    public List<AuditEntryDto> Entries { get; } = new();

    public Task InsertAsync(AuditEntryDto entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public sealed class FakeAppSettingsRepository : IAppSettingsRepository
{
    public Dictionary<string, string> Values { get; } = new();

    public Task<IReadOnlyDictionary<string, string>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<string, string>>(Values
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(kv => kv.Key[prefix.Length..], kv => kv.Value));

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        Values[key] = value;
        return Task.CompletedTask;
    }
}

/// <summary>A sign-in account, with the counter and lock that usp_Account_RecordFailedSignIn keeps.</summary>
public sealed class FakeAccount
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public string AccountType { get; init; } = "Agent";
    public string? AgentRole { get; init; } = "Agent";
    public string Name { get; init; } = "Nadia Hussain";
    public bool Active { get; set; } = true;
    public bool Locked { get; set; }
    public string? PasswordHash { get; set; }
    public int FailedSignIns { get; set; }
}

public sealed class FakeCredentialRepository : IAccountCredentialRepository
{
    public List<FakeAccount> Accounts { get; } = new();
    public List<string> FailedSignInsRecorded { get; } = new();

    private FakeAccount Find(string accountId) => Accounts.Single(a => a.Id == accountId);

    public Task<string?> GetPasswordHashAsync(string accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(Accounts.FirstOrDefault(a => a.Id == accountId)?.PasswordHash);

    public Task SetPasswordHashAsync(string accountId, string passwordHash, bool unlock, CancellationToken cancellationToken = default)
    {
        var account = Find(accountId);
        account.PasswordHash = passwordHash;
        if (unlock) account.Locked = false;
        return Task.CompletedTask;
    }

    public Task<SignInAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Task.FromResult(Accounts.FirstOrDefault(a => a.Username == username) is { } a
            ? new SignInAccount(a.AccountType, a.Id, a.Name, a.AgentRole, a.PasswordHash, a.Locked, a.Active)
            : null);

    // The rule usp_Account_RecordFailedSignIn applies: count, and lock on reaching lockAfter (0 = never).
    public Task<bool> RecordFailedSignInAsync(string accountId, int lockAfter, CancellationToken cancellationToken = default)
    {
        FailedSignInsRecorded.Add(accountId);
        var account = Find(accountId);
        account.FailedSignIns++;
        var justLocked = lockAfter > 0 && !account.Locked && account.FailedSignIns >= lockAfter;
        if (justLocked) account.Locked = true;
        return Task.FromResult(justLocked);
    }

    public Task ResetFailedSignInsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        Find(accountId).FailedSignIns = 0;
        return Task.CompletedTask;
    }

    public Task<AccountStatus?> GetStatusAsync(string accountId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<TestAccount>> ListForTestingAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
