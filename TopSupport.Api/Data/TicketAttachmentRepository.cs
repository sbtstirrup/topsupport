using Microsoft.Data.SqlClient;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of ITicketAttachmentRepository. usp_TicketAttachment_GetContent is
/// the only stored procedure allowed to select the Content column; every other method here
/// only ever touches metadata.
/// </summary>
public class TicketAttachmentRepository : ITicketAttachmentRepository
{
    private readonly IDbExecutor _db;

    public TicketAttachmentRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<TicketAttachmentDto> InsertAsync(
        string ticketId,
        string? timelineEntryId,
        string fileName,
        string contentType,
        byte[] content,
        TicketActorRef uploadedBy,
        DateTimeOffset uploadedAt,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("n");

        await _db.ExecuteNonQueryAsync(
            "dbo.usp_TicketAttachment_Insert",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@TimelineEntryId", (object?)timelineEntryId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FileName", fileName);
                cmd.Parameters.AddWithValue("@ContentType", contentType);
                cmd.Parameters.AddWithValue("@SizeBytes", (long)content.Length);
                cmd.Parameters.AddWithValue("@Content", content);
                cmd.Parameters.AddWithValue("@UploadedByType", uploadedBy.Type);
                cmd.Parameters.AddWithValue("@UploadedById", (object?)uploadedBy.Id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UploadedAt", uploadedAt);
            },
            cancellationToken);

        return new TicketAttachmentDto
        {
            Id = id,
            TicketId = ticketId,
            TimelineEntryId = timelineEntryId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = content.Length,
            UploadedByType = uploadedBy.Type,
            UploadedById = uploadedBy.Id,
            UploadedAt = uploadedAt
        };
    }

    public async Task<IReadOnlyList<TicketAttachmentDto>> GetMetadataByTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var attachments = new List<TicketAttachmentDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_TicketAttachment_GetMetadataByTicket",
            cmd => cmd.Parameters.AddWithValue("@TicketId", ticketId),
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    attachments.Add(ReadMetadata(reader));
                }
            });

        return attachments;
    }

    public async Task<TicketAttachmentContentDto?> GetContentAsync(string attachmentId, CancellationToken cancellationToken = default)
    {
        TicketAttachmentContentDto? content = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_TicketAttachment_GetContent",
            cmd => cmd.Parameters.AddWithValue("@AttachmentId", attachmentId),
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    content = new TicketAttachmentContentDto
                    {
                        Id = attachmentId,
                        FileName = (string)reader["FileName"],
                        ContentType = (string)reader["ContentType"],
                        Content = (byte[])reader["Content"]
                    };
                }
            });

        return content;
    }

    public async Task<string?> GetTicketIdAsync(string attachmentId, CancellationToken cancellationToken = default)
    {
        string? ticketId = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_TicketAttachment_GetTicketId",
            cmd => cmd.Parameters.AddWithValue("@AttachmentId", attachmentId),
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    ticketId = (string)reader["TicketId"];
                }
            });

        return ticketId;
    }

    private static TicketAttachmentDto ReadMetadata(SqlDataReader reader) => new()
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
