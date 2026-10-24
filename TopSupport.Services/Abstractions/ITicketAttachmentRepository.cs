using TopSupport.Models.Tickets;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.TicketAttachments. usp_TicketAttachment_GetContent is the
/// only stored procedure allowed to select the Content column — GetContentAsync is the only
/// method here that returns bytes; everything else is metadata-only.
/// </summary>
public interface ITicketAttachmentRepository
{
    Task<TicketAttachmentDto> InsertAsync(
        string ticketId,
        string? timelineEntryId,
        string fileName,
        string contentType,
        byte[] content,
        TicketActorRef uploadedBy,
        DateTimeOffset uploadedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketAttachmentDto>> GetMetadataByTicketAsync(string ticketId, CancellationToken cancellationToken = default);

    Task<TicketAttachmentContentDto?> GetContentAsync(string attachmentId, CancellationToken cancellationToken = default);
}
