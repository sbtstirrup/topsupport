using System.Text.Json;
using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of IAgentAdminRepository. The GetAll procs use the same multi-result-set
/// shapes as usp_Agent_GetAllActive/usp_Team_GetAll (grouped by id in C#); the Save procs take
/// departments/team ids/member ids as JSON string arrays, split with OPENJSON in SQL.
/// </summary>
public class AgentAdminRepository : IAgentAdminRepository
{
    private readonly IDbExecutor _db;

    public AgentAdminRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgentDto>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = new List<AgentDto>();
        var byId = new Dictionary<string, AgentDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminAgent_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var agent = ReadAgent(reader);
                    agents.Add(agent);
                    byId[agent.Id] = agent;
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    if (byId.TryGetValue((string)reader["AgentId"], out var agent))
                    {
                        agent.Departments.Add((string)reader["Department"]);
                    }
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    if (byId.TryGetValue((string)reader["AgentId"], out var agent))
                    {
                        agent.TeamIds.Add((string)reader["TeamId"]);
                    }
                }
            });

        // Departments in DEPTS order, as the demo's checkbox order produces them.
        foreach (var agent in agents)
        {
            agent.Departments = agent.Departments
                .OrderBy(d => IndexOfDepartment(d))
                .ToList();
        }

        return agents;
    }

    public async Task<IReadOnlyList<TeamDto>> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = new List<TeamDto>();
        var byId = new Dictionary<string, TeamDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminTeam_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var team = new TeamDto
                    {
                        Id = (string)reader["Id"],
                        Name = (string)reader["Name"],
                        LeadAgentId = reader["LeadAgentId"] as string ?? string.Empty,
                        Department = (string)reader["Department"]
                    };
                    teams.Add(team);
                    byId[team.Id] = team;
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    if (byId.TryGetValue((string)reader["TeamId"], out var team))
                    {
                        team.MemberAgentIds.Add((string)reader["AgentId"]);
                    }
                }
            });

        return teams;
    }

    public async Task<ActiveTicketCountsDto> GetActiveTicketCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = new ActiveTicketCountsDto();

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminAgent_GetActiveTicketCounts",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    counts.ByAgent[(string)reader["Id"]] = (int)reader["ActiveTickets"];
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    counts.ByTeam[(string)reader["Id"]] = (int)reader["ActiveTickets"];
                }
            });

        return counts;
    }

    public async Task<(bool EmailTaken, bool UsernameTaken)> FindConflictsAsync(string id, string email, string username, CancellationToken cancellationToken = default)
    {
        var result = (false, false);

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminAgent_FindConflicts",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Username", username);
            },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    result = ((bool)reader["EmailTaken"], (bool)reader["UsernameTaken"]);
                }
            });

        return result;
    }

    public Task SaveAgentAsync(AgentDto agent, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_AdminAgent_Save",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", agent.Id);
                cmd.Parameters.AddWithValue("@Name", agent.Name);
                cmd.Parameters.AddWithValue("@Username", agent.Username);
                cmd.Parameters.AddWithValue("@Email", agent.Email);
                cmd.Parameters.AddWithValue("@Phone", (object?)agent.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Role", agent.Role);
                cmd.Parameters.AddWithValue("@JobTitle", (object?)agent.JobTitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Active", agent.Active);
                cmd.Parameters.AddWithValue("@DepartmentsJson", JsonSerializer.Serialize(agent.Departments));
                cmd.Parameters.AddWithValue("@TeamIdsJson", JsonSerializer.Serialize(agent.TeamIds));
            },
            cancellationToken);

    public Task SaveTeamAsync(TeamDto team, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_AdminTeam_Save",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", team.Id);
                cmd.Parameters.AddWithValue("@Name", team.Name);
                cmd.Parameters.AddWithValue("@Department", team.Department);
                cmd.Parameters.AddWithValue("@LeadAgentId", team.LeadAgentId);
                cmd.Parameters.AddWithValue("@MemberIdsJson", JsonSerializer.Serialize(team.MemberAgentIds));
            },
            cancellationToken);

    public async Task<int> CountTeamTicketsAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var count = 0;

        await _db.ExecuteReaderAsync(
            "dbo.usp_AdminTeam_CountTickets",
            cmd => cmd.Parameters.AddWithValue("@Id", teamId),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    count = (int)reader["TicketCount"];
                }
            });

        return count;
    }

    public Task DeleteTeamAsync(string teamId, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_AdminTeam_Delete",
            cmd => cmd.Parameters.AddWithValue("@Id", teamId),
            cancellationToken);

    private static int IndexOfDepartment(string department)
    {
        var index = TicketTopics.DEPTS.ToList().IndexOf(department);
        return index < 0 ? int.MaxValue : index;
    }

    private static AgentDto ReadAgent(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        Name = (string)reader["Name"],
        Username = (string)reader["Username"],
        Email = (string)reader["Email"],
        Phone = reader["Phone"] as string,
        Role = (string)reader["Role"],
        JobTitle = reader["JobTitle"] as string,
        Active = (bool)reader["Active"],
        Locked = (bool)reader["Locked"]
    };
}
