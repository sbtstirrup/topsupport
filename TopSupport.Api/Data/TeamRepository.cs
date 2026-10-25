using Microsoft.Data.SqlClient;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of ITeamRepository. Both usp_Team_GetById and usp_Team_GetAll
/// return 2 result sets: team row(s); (TeamId, AgentId) rows from TeamMembers, grouped by
/// TeamId in C# to populate each TeamDto's MemberAgentIds.
/// </summary>
public class TeamRepository : ITeamRepository
{
    private readonly IDbExecutor _db;

    public TeamRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<TeamDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        TeamDto? team = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Team_GetById",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    team = ReadTeam(reader);
                }
            },
            async (reader, ct) =>
            {
                if (team is null) return;
                while (await reader.ReadAsync(ct))
                {
                    team.MemberAgentIds.Add((string)reader["AgentId"]);
                }
            });

        return team;
    }

    public async Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var teams = new List<TeamDto>();
        var byId = new Dictionary<string, TeamDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Team_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var team = ReadTeam(reader);
                    teams.Add(team);
                    byId[team.Id] = team;
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var teamId = (string)reader["TeamId"];
                    if (byId.TryGetValue(teamId, out var team))
                    {
                        team.MemberAgentIds.Add((string)reader["AgentId"]);
                    }
                }
            });

        return teams;
    }

    private static TeamDto ReadTeam(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        Name = (string)reader["Name"],
        LeadAgentId = reader["LeadAgentId"] as string ?? string.Empty,
        Department = (string)reader["Department"]
    };
}
