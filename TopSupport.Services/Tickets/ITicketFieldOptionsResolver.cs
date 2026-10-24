using TopSupport.Models.Tickets.FieldSchema;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Resolves the dynamic-option fields in a topic's field schema (TicketFieldOptionsSource !=
/// Static) against live data — the affected user's assets, and the active-users pool — before
/// the schema is handed to a client. Mirrors the runtime option-building inside topicFields()
/// (mine/laptops/userOpts expressions) in topsupport-app.js.
/// </summary>
public interface ITicketFieldOptionsResolver
{
    /// <summary>
    /// Returns the topic's field list with every dynamic-option field's StaticOptions filled
    /// in (and Hint/EmptyMessage text completed where the demo computes it per-user), given
    /// the affected user id and the currently-submitted field values (needed for
    /// deviceType-dependent asset filtering on the hardware topic).
    /// </summary>
    Task<List<TicketFieldDescriptor>> ResolveAsync(
        string topic,
        string? affectedUserId,
        IReadOnlyDictionary<string, object?> currentFields,
        CancellationToken cancellationToken = default);
}
