namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// Flags the handful of fields in topicFields() whose option list depends on runtime data
/// (the affected user's assets) rather than being static. Resolved server-side by
/// TicketFieldOptionsResolver before a field schema is sent to a client.
/// </summary>
public enum TicketFieldOptionsSource
{
    /// <summary>Options come entirely from TicketFieldDescriptor.StaticOptions.</summary>
    Static,

    /// <summary>Hardware topic's "device" field: affected user's assets filtered to the
    /// asset type implied by the selected deviceType (DEVICE_ASSET map).</summary>
    AffectedUserAssetsByDeviceType,

    /// <summary>Leaver topic's "returnAssets" field: all of the affected user's assets.</summary>
    AffectedUserAssets,

    /// <summary>VPN topic's "vpnDevice" field: affected user's laptop-type assets only.</summary>
    AffectedUserLaptops,

    /// <summary>Leaver topic's "lineManager"/"mailboxTarget" fields: active users excluding
    /// the affected user (userOpts().filter(o => o.v !== aff) in the demo).</summary>
    ActiveUsersExcludingAffected,

    /// <summary>New-user topic's "lineManager" field: all active users (userOpts()).</summary>
    ActiveUsersAll
}
