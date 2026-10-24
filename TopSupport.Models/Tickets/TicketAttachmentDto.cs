namespace TopSupport.Models.Tickets;

/// <summary>
/// Metadata for one row in TicketAttachments — deliberately never carries the Content bytes
/// (see usp_TicketAttachment_GetMetadataByTicket, the only proc other than
/// usp_TicketAttachment_GetContent allowed to touch that table, and it never selects Content).
/// </summary>
public class TicketAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Null when the file was attached at ticket-creation time, before any timeline
    /// entry existed to anchor it to.</summary>
    public string? TimelineEntryId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>"user" | "agent" | "system".</summary>
    public string UploadedByType { get; set; } = string.Empty;
    public string? UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// Full attachment content, returned only by the dedicated download path
/// (usp_TicketAttachment_GetContent) — never bundled into TicketDto/TicketAttachmentDto.
/// </summary>
public class TicketAttachmentContentDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
