using System.Text.Json;
using Microsoft.Data.SqlClient;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of ITicketRepository, calling the stored procedures in
/// TopSupport.Database/Scripts/03_StoredProcedures_Core.sql and
/// 05_StoredProcedures_Extensions.sql directly — no EF Core, no Dapper.
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly DbExecutor _db;

    public TicketRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<TicketDto?> GetByIdAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        TicketDto? ticket = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_GetById",
            cmd => cmd.Parameters.AddWithValue("@TicketId", ticketId),
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    ticket = ReadTicket(reader);
                }
            },
            async (reader, ct) =>
            {
                if (ticket is null) return;
                while (await reader.ReadAsync(ct))
                {
                    ticket.Timeline.Add(ReadTimelineEntry(reader));
                }
            },
            async (reader, ct) =>
            {
                if (ticket is null) return;
                while (await reader.ReadAsync(ct))
                {
                    ticket.Tasks.Add(ReadTask(reader));
                }
            },
            async (reader, ct) =>
            {
                if (ticket is null) return;
                while (await reader.ReadAsync(ct))
                {
                    ticket.ViewerAgentIds.Add(reader.GetString(0));
                }
            },
            async (reader, ct) =>
            {
                if (ticket is null) return;
                while (await reader.ReadAsync(ct))
                {
                    ticket.Attachments.Add(ReadAttachmentMetadata(reader));
                }
            });

        return ticket;
    }

    public async Task<IReadOnlyList<TicketDto>> GetByRequesterOrAffectedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tickets = new List<TicketDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_GetByRequesterOrAffected",
            cmd => cmd.Parameters.AddWithValue("@UserId", userId),
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    tickets.Add(ReadTicket(reader));
                }
            });

        return tickets;
    }

    public async Task<TicketDto> CreateAsync(TicketDto ticket, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteNonQueryAsync(
            "dbo.usp_Ticket_Create",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", ticket.Id);
                cmd.Parameters.AddWithValue("@Number", ticket.Number);
                cmd.Parameters.AddWithValue("@Subject", ticket.Subject);
                cmd.Parameters.AddWithValue("@Topic", ticket.Topic);
                cmd.Parameters.AddWithValue("@Department", ticket.Department);
                cmd.Parameters.AddWithValue("@Status", ticket.Status.ToString());
                cmd.Parameters.AddWithValue("@Priority", ticket.Priority.ToString());
                cmd.Parameters.AddWithValue("@RequesterUserId", ticket.RequesterUserId);
                cmd.Parameters.AddWithValue("@AffectedUserId", ticket.AffectedUserId);
                cmd.Parameters.AddWithValue("@CreatedByType", ticket.CreatedBy.Type);
                cmd.Parameters.AddWithValue("@CreatedById", (object?)ticket.CreatedBy.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Created", ticket.Created);
                cmd.Parameters.AddWithValue("@Updated", ticket.Updated);
                cmd.Parameters.AddWithValue("@AssignedAgentId", (object?)ticket.AssignedAgentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTeamId", (object?)ticket.AssignedTeamId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FieldsJson", JsonSerializer.Serialize(ticket.Fields));
            },
            cancellationToken);

        return ticket;
    }

    public Task UpdateStatusAsync(string ticketId, string status, DateTimeOffset updated, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Ticket_UpdateStatus",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Updated", updated);
            },
            cancellationToken);

    public Task UpdatePriorityAsync(string ticketId, string priority, DateTimeOffset updated, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Ticket_UpdatePriority",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@Priority", priority);
                cmd.Parameters.AddWithValue("@Updated", updated);
            },
            cancellationToken);

    public Task UpdateTopicAsync(string ticketId, string topic, string department, DateTimeOffset updated, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Ticket_UpdateTopic",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@Topic", topic);
                cmd.Parameters.AddWithValue("@Department", department);
                cmd.Parameters.AddWithValue("@Updated", updated);
            },
            cancellationToken);

    public Task AssignAsync(string ticketId, string? assignedAgentId, string? assignedTeamId, DateTimeOffset updated, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_Ticket_Assign",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@AssignedAgentId", (object?)assignedAgentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTeamId", (object?)assignedTeamId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Updated", updated);
            },
            cancellationToken);

    public async Task<bool> ClaimIfUnchangedAsync(string ticketId, string agentId, string? assignedTeamId, string? expectedAgentId, DateTimeOffset updated, CancellationToken cancellationToken = default)
    {
        var affected = 0;
        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_ClaimIfUnchanged",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@AgentId", agentId);
                cmd.Parameters.AddWithValue("@AssignedTeamId", (object?)assignedTeamId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ExpectedAgentId", (object?)expectedAgentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Updated", updated);
            },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    affected = reader.GetInt32(reader.GetOrdinal("Affected"));
                }
            });
        return affected > 0;
    }

    public async Task<int> GetNextNumberAsync(CancellationToken cancellationToken = default)
    {
        int next = 0;
        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_NextNumber",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    next = reader.GetInt32(reader.GetOrdinal("NextNumber"));
                }
            });
        return next;
    }

    public async Task<TicketTimelineEntryDto> AppendTimelineEntryAsync(TicketTimelineEntryDto entry, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteNonQueryAsync(
            "dbo.usp_TicketTimelineEntry_Insert",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", entry.Id);
                cmd.Parameters.AddWithValue("@TicketId", entry.TicketId);
                cmd.Parameters.AddWithValue("@Kind", entry.Kind.ToString());
                cmd.Parameters.AddWithValue("@At", entry.At);
                cmd.Parameters.AddWithValue("@ByType", entry.By.Type);
                cmd.Parameters.AddWithValue("@ById", (object?)entry.By.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Text", (object?)entry.Text ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Html", (object?)entry.Html ?? DBNull.Value);
            },
            cancellationToken);

        return entry;
    }

    public async Task<TicketTaskDto> InsertTaskAsync(TicketTaskDto task, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteNonQueryAsync(
            "dbo.usp_TicketTask_Insert",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", task.Id);
                cmd.Parameters.AddWithValue("@TicketId", task.TicketId);
                cmd.Parameters.AddWithValue("@Text", task.Text);
                cmd.Parameters.AddWithValue("@AssigneeAgentId", task.AssigneeAgentId);
                // Explicitly DATE (not AddWithValue's DATETIME inference), so any DateOnly fits
                // instead of dates before 1753 throwing SqlDateTime overflow.
                cmd.Parameters.Add("@Due", System.Data.SqlDbType.Date).Value =
                    task.Due.HasValue ? task.Due.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
                cmd.Parameters.AddWithValue("@Done", task.Done);
            },
            cancellationToken);

        return task;
    }

    public Task SetTaskDoneAsync(string taskId, bool done, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_TicketTask_SetDone",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@TaskId", taskId);
                cmd.Parameters.AddWithValue("@Done", done);
            },
            cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetActiveForQueueAsync(CancellationToken cancellationToken = default)
    {
        var tickets = new List<TicketDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_GetActiveForQueue",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    tickets.Add(ReadQueueTicket(reader));
                }
            });

        return tickets;
    }

    public async Task<IReadOnlyList<TicketDto>> SearchForQueueAsync(
        string? status,
        string? priority,
        string? topic,
        string? department,
        string? assignedAgentId,
        string? assignedTeamId,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        var tickets = new List<TicketDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Ticket_SearchForQueue",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Priority", (object?)priority ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Topic", (object?)topic ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedAgentId", (object?)assignedAgentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTeamId", (object?)assignedTeamId ?? DBNull.Value);
                // The proc wraps @SearchText in LIKE '%...%': escape the pattern characters so a
                // search for "50%" or "a_b" matches literally ("[" first, before it's reused).
                cmd.Parameters.AddWithValue("@SearchText", searchText is null ? DBNull.Value
                    : (object)searchText.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]"));
            },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    tickets.Add(ReadQueueTicket(reader));
                }
            });

        return tickets;
    }

    private static TicketDto ReadTicket(SqlDataReader reader)
    {
        var fieldsJson = reader["FieldsJson"] as string ?? "{}";

        return new TicketDto
        {
            Id = (string)reader["Id"],
            Number = (string)reader["Number"],
            Subject = (string)reader["Subject"],
            Topic = (string)reader["Topic"],
            Department = (string)reader["Department"],
            Status = Enum.Parse<TicketStatus>((string)reader["Status"]),
            Priority = Enum.Parse<TicketPriority>((string)reader["Priority"]),
            RequesterUserId = (string)reader["RequesterUserId"],
            AffectedUserId = (string)reader["AffectedUserId"],
            CreatedBy = new TicketActorRef
            {
                Type = (string)reader["CreatedByType"],
                Id = reader["CreatedById"] as string
            },
            Created = (DateTimeOffset)reader["Created"],
            Updated = (DateTimeOffset)reader["Updated"],
            AssignedAgentId = reader["AssignedAgentId"] as string,
            AssignedTeamId = reader["AssignedTeamId"] as string,
            Fields = JsonSerializer.Deserialize<Dictionary<string, object?>>(fieldsJson) ?? new()
        };
    }

    private static TicketDto ReadQueueTicket(SqlDataReader reader)
    {
        var ticket = ReadTicket(reader);
        ticket.FirstAgentReplyAt = reader["FirstAgentReplyAt"] as DateTimeOffset?;
        ticket.LastReplyByType = reader["LastReplyByType"] as string;
        return ticket;
    }

    private static TicketTimelineEntryDto ReadTimelineEntry(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        TicketId = (string)reader["TicketId"],
        Kind = Enum.Parse<TicketTimelineEntryKind>((string)reader["Kind"], ignoreCase: true),
        At = (DateTimeOffset)reader["At"],
        By = new TicketActorRef
        {
            Type = (string)reader["ByType"],
            Id = reader["ById"] as string
        },
        Text = reader["Text"] as string,
        Html = reader["Html"] as string
    };

    private static TicketTaskDto ReadTask(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        TicketId = (string)reader["TicketId"],
        Text = (string)reader["Text"],
        AssigneeAgentId = (string)reader["AssigneeAgentId"],
        Due = reader["Due"] is DateTime due ? DateOnly.FromDateTime(due) : null,
        Done = (bool)reader["Done"]
    };

    private static TicketAttachmentDto ReadAttachmentMetadata(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        TicketId = (string)reader["TicketId"],
        TimelineEntryId = reader["TimelineEntryId"] as string,
        FileName = (string)reader["FileName"],
        ContentType = (string)reader["ContentType"],
        SizeBytes = Convert.ToInt64(reader["SizeBytes"]),
        UploadedByType = (string)reader["UploadedByType"],
        UploadedById = reader["UploadedById"] as string,
        UploadedAt = (DateTimeOffset)reader["UploadedAt"]
    };
}
