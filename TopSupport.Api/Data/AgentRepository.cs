using Microsoft.Data.SqlClient;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of IAgentRepository. Both usp_Agent_GetById and
/// usp_Agent_GetAllActive return 3 result sets: core agent row(s); (AgentId, Department)
/// rows from AgentDepartments; (TeamId, AgentId) rows from TeamMembers. For the "AllActive"
/// case the 2nd/3rd sets are grouped by AgentId in C# to populate each AgentDto's
/// Departments/TeamIds lists.
/// </summary>
public class AgentRepository : IAgentRepository
{
    private readonly IDbExecutor _db;

    public AgentRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<AgentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        AgentDto? agent = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Agent_GetById",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    agent = ReadAgent(reader);
                }
            },
            async (reader, ct) =>
            {
                if (agent is null) return;
                while (await reader.ReadAsync(ct))
                {
                    agent.Departments.Add((string)reader["Department"]);
                }
            },
            async (reader, ct) =>
            {
                if (agent is null) return;
                while (await reader.ReadAsync(ct))
                {
                    agent.TeamIds.Add((string)reader["TeamId"]);
                }
            });

        return agent;
    }

    public async Task<IReadOnlyList<AgentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var agents = new List<AgentDto>();
        var byId = new Dictionary<string, AgentDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Agent_GetAllActive",
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
                    var agentId = (string)reader["AgentId"];
                    if (byId.TryGetValue(agentId, out var agent))
                    {
                        agent.Departments.Add((string)reader["Department"]);
                    }
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var agentId = (string)reader["AgentId"];
                    if (byId.TryGetValue(agentId, out var agent))
                    {
                        agent.TeamIds.Add((string)reader["TeamId"]);
                    }
                }
            });

        return agents;
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
