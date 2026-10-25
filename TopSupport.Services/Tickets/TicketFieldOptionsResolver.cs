using TopSupport.Models.People;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Fills in each topic field's options for the affected user, porting the runtime option-building expressions
/// inside topicFields() (topsupport-app.js lines ~879-951): userAssets(aff)/filter-by-type,
/// userOpts().filter(o => o.v !== aff), and the per-field hint/empty-message text that
/// depends on the affected user's name.
/// </summary>
public class TicketFieldOptionsResolver
{
    private readonly IUserRepository _users;
    private readonly IAssetRepository _assets;

    public TicketFieldOptionsResolver(IUserRepository users, IAssetRepository assets)
    {
        _users = users;
        _assets = assets;
    }

    /// <summary>
    /// Returns the topic's field list with every dynamic-option field's StaticOptions filled
    /// in (and Hint/EmptyMessage text completed where the demo computes it per-user), given
    /// the affected user id and the currently-submitted field values (needed for
    /// deviceType-dependent asset filtering on the hardware topic).
    /// </summary>
    public async Task<List<TicketFieldDescriptor>> ResolveAsync(
        string topic,
        string? affectedUserId,
        IReadOnlyDictionary<string, object?> currentFields,
        CancellationToken cancellationToken = default)
    {
        var fields = TicketTopicFieldSchemas.GetFields(topic);

        UserDto? affectedUser = string.IsNullOrEmpty(affectedUserId)
            ? null
            : await _users.GetByIdAsync(affectedUserId, cancellationToken);
        var affectedName = affectedUser?.Name ?? "the affected user";

        var affectedAssets = string.IsNullOrEmpty(affectedUserId)
            ? Array.Empty<AssetDto>()
            : (await _assets.GetByUserIdAsync(affectedUserId, cancellationToken)).ToArray();

        List<UserDto>? activeUsers = null;

        foreach (var field in fields)
        {
            switch (field.OptionsSource)
            {
                case TicketFieldOptionsSource.AffectedUserAssetsByDeviceType:
                {
                    var deviceType = currentFields.TryGetValue("deviceType", out var dt) ? dt?.ToString() : null;
                    var matching = deviceType != null && TicketFieldOptionSets.DEVICE_ASSET.TryGetValue(deviceType, out var assetType)
                        ? affectedAssets.Where(a => a.Type == assetType).ToList()
                        : new List<AssetDto>();

                    field.StaticOptions = matching
                        .Select(a => new TicketFieldOption { Value = a.Tag, Label = AssetLabel(a) })
                        .Append(new TicketFieldOption { Value = "notlisted", Label = "Not listed / don't know" })
                        .ToList();

                    field.Hint = matching.Count > 0
                        ? $"Devices of this type assigned to {affectedName}."
                        : $"No {(string.IsNullOrEmpty(deviceType) ? "device" : deviceType.ToLowerInvariant())} is assigned to {affectedName} in the asset list.";
                    break;
                }

                case TicketFieldOptionsSource.AffectedUserAssets:
                    field.StaticOptions = affectedAssets
                        .Select(a => new TicketFieldOption { Value = a.Tag, Label = AssetLabel(a) })
                        .ToList();
                    field.Hint = $"Equipment assigned to {affectedName}.";
                    field.EmptyMessage = $"{affectedName} has no equipment in the asset list.";
                    break;

                case TicketFieldOptionsSource.AffectedUserLaptops:
                    field.StaticOptions = affectedAssets
                        .Where(a => a.Type == "laptop")
                        .Select(a => new TicketFieldOption { Value = a.Tag, Label = AssetLabel(a) })
                        .Append(new TicketFieldOption { Value = "other", Label = "Another TopCashback device (tell us in the reason)" })
                        .ToList();
                    break;

                case TicketFieldOptionsSource.ActiveUsersExcludingAffected:
                    activeUsers ??= (await _users.GetAllActiveAsync(cancellationToken)).OrderBy(u => u.Name, StringComparer.Ordinal).ToList();
                    field.StaticOptions = activeUsers
                        .Where(u => u.Id != affectedUserId)
                        .Select(u => new TicketFieldOption { Value = u.Id, Label = $"{u.Name} ({u.Department})" })
                        .ToList();
                    break;

                case TicketFieldOptionsSource.ActiveUsersAll:
                    activeUsers ??= (await _users.GetAllActiveAsync(cancellationToken)).OrderBy(u => u.Name, StringComparer.Ordinal).ToList();
                    field.StaticOptions = activeUsers
                        .Select(u => new TicketFieldOption { Value = u.Id, Label = $"{u.Name} ({u.Department})" })
                        .ToList();
                    break;
            }
        }

        // leaver.mailboxTarget's label flips between "Forward emails to" and "Give mailbox
        // access to" depending on the current `mailbox` value — see the demo's ternary.
        if (topic == "leaver")
        {
            var mailboxTarget = fields.FirstOrDefault(f => f.Key == "mailboxTarget");
            if (mailboxTarget != null)
            {
                var mailbox = currentFields.TryGetValue("mailbox", out var mb) ? mb?.ToString() : null;
                mailboxTarget.Label = mailbox == "Delegate access" ? "Give mailbox access to" : "Forward emails to";
            }
        }

        // hardware.location's per-option descriptions depend on the affected user's office.
        if (topic == "hardware")
        {
            var location = fields.FirstOrDefault(f => f.Key == "location");
            var deskOption = location?.StaticOptions?.FirstOrDefault(o => o.Value == "desk");
            if (deskOption != null)
            {
                deskOption.Description = affectedUser?.Office ?? "Choose the affected user first.";
            }
        }

        return fields;
    }

    private static string AssetLabel(AssetDto asset) => $"{asset.Tag} · {asset.Model}";
}
