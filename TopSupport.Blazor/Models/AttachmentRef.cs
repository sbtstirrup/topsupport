namespace TopSupport.Blazor.Models;

/// <summary>
/// Lightweight client-side view of an attachment used by FileUploadDropzone and the forms that
/// compose it. Id is null for a file staged locally but not yet uploaded (e.g. the raise-ticket
/// form before the ticket exists); once uploaded it carries the server's TicketAttachmentDto.Id.
/// </summary>
public record AttachmentRef(string? Id, string Name, long SizeBytes, string ContentType);
