using TopSupport.Models.Admin;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Ticket file attachments: upload (validated against TicketAttachmentRules), the metadata list,
/// and the content download. The stored content type always comes from the allow-listed
/// extension, never from the client.
/// </summary>
public class TicketAttachmentService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketAttachmentRepository _attachmentRepository;
    private readonly AuditService _audit;
    private readonly TimeProvider _time;

    public TicketAttachmentService(
        ITicketRepository ticketRepository,
        ITicketAttachmentRepository attachmentRepository,
        AuditService audit,
        TimeProvider time)
    {
        _ticketRepository = ticketRepository;
        _attachmentRepository = attachmentRepository;
        _audit = audit;
        _time = time;
    }

    /// <summary>
    /// Validates and stores one file: the ticket must exist (KeyNotFoundException) and not be
    /// Closed; the extension must be allow-listed, the file non-empty and at most 10 MB, and the
    /// ticket under its file-count/total-size caps; a timelineEntryId must be on the same ticket
    /// and, for an end user, one of their own replies. Rule failures throw ValidationException.
    /// The file name is sanitised. Audited as "Attached file".
    /// </summary>
    public async Task<TicketAttachmentDto> AddAsync(
        string ticketId,
        string? timelineEntryId,
        string fileName,
        byte[] content,
        TicketActorRef uploadedBy,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket '{ticketId}' was not found.");
        if (ticket.Status == TicketStatus.Closed)
        {
            throw new ValidationException("", "This ticket is closed.");
        }

        var safeName = TicketAttachmentRules.SanitizeFileName(fileName);
        var storedContentType = TicketAttachmentRules.ContentTypeFor(safeName)
            ?? throw new ValidationException("file", TicketAttachmentRules.TypeNotAllowedMessage);

        if (content.LongLength == 0)
        {
            throw new ValidationException("file", TicketAttachmentRules.EmptyFileMessage);
        }

        if (content.LongLength > TicketAttachmentRules.MaxFileSizeBytes)
        {
            throw new ValidationException("file", $"'{safeName}' is over 10 MB and was not added.");
        }

        if (ticket.Attachments.Count >= TicketAttachmentRules.MaxFilesPerTicket)
        {
            throw new ValidationException("file", TicketAttachmentRules.TooManyFilesMessage);
        }

        if (ticket.Attachments.Sum(a => a.SizeBytes) + content.LongLength > TicketAttachmentRules.MaxTotalBytesPerTicket)
        {
            throw new ValidationException("file", TicketAttachmentRules.TotalTooLargeMessage);
        }

        if (!string.IsNullOrEmpty(timelineEntryId))
        {
            // The anchor must be on this ticket; an end user may only anchor to one of their own replies.
            var entry = ticket.Timeline.FirstOrDefault(e => e.Id == timelineEntryId);
            var allowed = entry is not null &&
                (uploadedBy.Type != TicketActorTypes.User ||
                 (entry.Kind == TicketTimelineEntryKind.Reply && entry.By.Type == TicketActorTypes.User && entry.By.Id == uploadedBy.Id));
            if (!allowed)
            {
                throw new ValidationException("timelineEntryId", "That file can't be attached to that message.");
            }
        }
        else
        {
            timelineEntryId = null;
        }

        var attachment = await _attachmentRepository.InsertAsync(
            ticketId, timelineEntryId, safeName, storedContentType, content, uploadedBy, _time.GetUtcNow(), cancellationToken);

        var auditActor = uploadedBy.Type == TicketActorTypes.System || uploadedBy.Id == null ? TicketActorTypes.System : uploadedBy.Id;
        await _audit.LogAsync(auditActor, "Attached file", "#" + ticket.Number, AuditCategories.Ticket, cancellationToken);
        return attachment;
    }

    public Task<IReadOnlyList<TicketAttachmentDto>> GetForTicketAsync(string ticketId, CancellationToken cancellationToken = default)
        => _attachmentRepository.GetMetadataByTicketAsync(ticketId, cancellationToken);

    public Task<TicketAttachmentContentDto?> GetContentAsync(string attachmentId, CancellationToken cancellationToken = default)
        => _attachmentRepository.GetContentAsync(attachmentId, cancellationToken);

    /// <summary>The ticket an attachment belongs to, or null when the attachment doesn't exist.</summary>
    public Task<string?> GetTicketIdAsync(string attachmentId, CancellationToken cancellationToken = default)
        => _attachmentRepository.GetTicketIdAsync(attachmentId, cancellationToken);
}
