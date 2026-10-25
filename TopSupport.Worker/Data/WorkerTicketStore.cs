using System.Data;
using Microsoft.Data.SqlClient;
using TopSupport.Models.Enums;

namespace TopSupport.Worker.Data;

/// <summary>ADO.NET implementation of IWorkerTicketStore over the worker.* procs in 15_Worker.sql.</summary>
public sealed class WorkerTicketStore : IWorkerTicketStore
{
    private readonly WorkerDb _db;

    public WorkerTicketStore(WorkerDb db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TicketEvent>> GetEventsAsync(DateTimeOffset sinceUtc, DateTimeOffset untilUtc, CancellationToken cancellationToken)
    {
        var events = new List<TicketEvent>();
        await _db.ReadAsync("worker.usp_TicketEvents_GetSince", p =>
        {
            p.Add("@SinceUtc", SqlDbType.DateTimeOffset).Value = sinceUtc;
            p.Add("@UntilUtc", SqlDbType.DateTimeOffset).Value = untilUtc;
        }, cancellationToken, async reader =>
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                events.Add(new TicketEvent(
                    Enum.Parse<TicketEventType>((string)reader["EventType"], ignoreCase: true),
                    (string)reader["EventId"],
                    (DateTimeOffset)reader["At"],
                    reader["EventText"] as string,
                    reader["ReplyHtml"] as string,
                    (string)reader["TicketId"],
                    (string)reader["Number"],
                    (string)reader["Subject"],
                    Enum.Parse<TicketStatus>((string)reader["Status"]),
                    (string)reader["RequesterName"],
                    (string)reader["RequesterEmail"],
                    reader["AgentId"] as string,
                    reader["AgentName"] as string,
                    reader["AgentEmail"] as string));
            }
        });
        return events;
    }

    public async Task<(IReadOnlyList<SlaCandidate> Tickets, IReadOnlyList<TeamMember> TeamMembers)> GetSlaCandidatesAsync(CancellationToken cancellationToken)
    {
        var tickets = new List<SlaCandidate>();
        var members = new List<TeamMember>();
        await _db.ReadAsync("worker.usp_SlaSweep_GetCandidates", _ => { }, cancellationToken,
            async reader =>
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    tickets.Add(new SlaCandidate(
                        (string)reader["TicketId"],
                        (string)reader["Number"],
                        (string)reader["Subject"],
                        Enum.Parse<TicketStatus>((string)reader["Status"]),
                        Enum.Parse<TicketPriority>((string)reader["Priority"]),
                        (DateTimeOffset)reader["Created"],
                        reader["FirstAgentReplyAt"] as DateTimeOffset?,
                        reader["AgentName"] as string,
                        reader["AgentEmail"] as string,
                        reader["TeamId"] as string,
                        reader["TeamName"] as string));
                }
            },
            async reader => members.AddRange(await ReadTeamMembersAsync(reader, cancellationToken)));
        return (tickets, members);
    }

    public async Task<IReadOnlyList<ResolvedTicket>> GetResolvedTicketsAsync(CancellationToken cancellationToken)
    {
        var tickets = new List<ResolvedTicket>();
        await _db.ReadAsync("worker.usp_AutoClose_GetResolved", _ => { }, cancellationToken, async reader =>
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                tickets.Add(new ResolvedTicket((string)reader["TicketId"], (string)reader["Number"], (DateTimeOffset)reader["ResolvedAt"]));
            }
        });
        return tickets;
    }

    public Task<bool> CloseResolvedTicketAsync(string ticketId, DateTimeOffset atUtc, CancellationToken cancellationToken)
        => _db.ScalarAsync<bool>("worker.usp_AutoClose_CloseTicket", p =>
        {
            p.AddWithValue("@TicketId", ticketId);
            p.Add("@AtUtc", SqlDbType.DateTimeOffset).Value = atUtc;
            // Same id formats as TicketService (timeline) and AuditService ("au" + guid).
            p.AddWithValue("@EntryId", Guid.NewGuid().ToString("n"));
            p.AddWithValue("@AuditId", "au" + Guid.NewGuid().ToString("n"));
        }, cancellationToken);

    public async Task<(IReadOnlyList<DigestTeam> Teams, IReadOnlyList<DigestTicket> Tickets)> GetDigestAsync(CancellationToken cancellationToken)
    {
        var teams = new List<DigestTeam>();
        var tickets = new List<DigestTicket>();
        await _db.ReadAsync("worker.usp_Digest_Get", _ => { }, cancellationToken,
            async reader =>
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    teams.Add(new DigestTeam((string)reader["TeamId"], (string)reader["TeamName"], (string)reader["LeadName"], (string)reader["LeadEmail"]));
                }
            },
            async reader =>
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    tickets.Add(new DigestTicket(
                        (string)reader["TicketId"],
                        (string)reader["Number"],
                        (string)reader["Subject"],
                        Enum.Parse<TicketStatus>((string)reader["Status"]),
                        Enum.Parse<TicketPriority>((string)reader["Priority"]),
                        (DateTimeOffset)reader["Created"],
                        (string)reader["TeamId"],
                        reader["FirstAgentReplyAt"] as DateTimeOffset?,
                        reader["AgentName"] as string));
                }
            });
        return (teams, tickets);
    }

    private static async Task<List<TeamMember>> ReadTeamMembersAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var members = new List<TeamMember>();
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(new TeamMember((string)reader["TeamId"], (string)reader["AgentId"], (string)reader["Name"], (string)reader["Email"]));
        }

        return members;
    }
}
