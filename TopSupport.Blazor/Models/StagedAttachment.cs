using Microsoft.AspNetCore.Components.Forms;
using TopSupport.Models.Tickets;

namespace TopSupport.Blazor.Models;

/// <summary>
/// A file picked in a FileUploadDropzone but not uploaded yet (the ticket, or the reply/note it
/// will be anchored to, doesn't exist yet). The bytes are read when the file is picked: an
/// IBrowserFile can't be read once its InputFile changes again (a second pick replaces the
/// browser's file list), so holding the reference until submit would lose earlier picks.
/// Removing the staged entry before sending simply drops it -- nothing is uploaded.
/// </summary>
public sealed record StagedAttachment(AttachmentRef Reference, byte[] Content)
{
    public static async Task<StagedAttachment> ReadAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        await using var source = file.OpenReadStream(TicketAttachmentRules.MaxFileSizeBytes, cancellationToken);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        return new StagedAttachment(new AttachmentRef(null, file.Name, file.Size, file.ContentType), buffer.ToArray());
    }
}
