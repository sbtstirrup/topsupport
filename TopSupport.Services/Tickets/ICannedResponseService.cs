using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>Thrown by ICannedResponseService create/update when the form is invalid. Mapped to 400 by the Api.</summary>
public class CannedResponseValidationException : Exception
{
    /// <summary>Field key ("title" / "body") -> message, like the demo's F.errs in ACT['cn-save'].</summary>
    public IReadOnlyDictionary<string, string> Errors { get; }

    public CannedResponseValidationException(IReadOnlyDictionary<string, string> errors)
        : base("The canned response has validation errors.")
    {
        Errors = errors;
    }
}

/// <summary>
/// CRUD over ICannedResponseRepository (validated as ACT['cn-save'] does) plus FillPlaceholders,
/// porting fillPlaceholders(text, t) from topsupport-app.js (~line 1546-1551).
/// </summary>
public interface ICannedResponseService
{
    Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    // Create/Update/Delete audit as the demo's ACT['cn-save']/ACT['cn-del'] do ("Created/Edited/
    // Deleted canned response", target = title), attributed to actorId ("system" when null).

    /// <summary>
    /// Creates a response with a server-generated id (any client-supplied Id is ignored). Throws
    /// <see cref="CannedResponseValidationException"/> on invalid input.
    /// </summary>
    Task<CannedResponseDto> CreateAsync(CannedResponseDto response, string? actorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns false (nothing written or audited) when there's no such id. Throws
    /// <see cref="CannedResponseValidationException"/> on invalid input.
    /// </summary>
    Task<bool> UpdateAsync(CannedResponseDto response, string? actorId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns false (nothing written or audited) when there's no such id.</summary>
    Task<bool> DeleteAsync(string id, string? actorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Substitutes the 11 {ticket.*}/{recipient.*}/{agent.*} tokens in <paramref name="body"/>.
    /// When <paramref name="ticket"/> is null, uses the demo's exact fallback sample values
    /// (ticket #060012, requester "Amira Patel", etc.) so a preview still renders something
    /// sensible before a real ticket exists.
    /// </summary>
    Task<string> FillPlaceholdersAsync(string body, TicketDto? ticket, string? agentId, CancellationToken cancellationToken = default);
}
