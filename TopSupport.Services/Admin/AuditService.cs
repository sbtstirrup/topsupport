using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Port of audit(action, target, cat) in topsupport-app.js. Every admin mutation (user/org/agent/
/// team/permission/security/KB/site changes) calls LogAsync with the acting account's id, the same
/// action wording the demo uses, and one of <see cref="AuditCategories"/>.
/// </summary>
public class AuditService
{
    private readonly IAuditRepository _repository;

    public AuditService(IAuditRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(CancellationToken cancellationToken = default)
        => _repository.GetRecentAsync(cancellationToken);

    public Task LogAsync(string actorId, string action, string target, string category, CancellationToken cancellationToken = default)
    {
        if (!AuditCategories.All.Contains(category))
        {
            throw new ArgumentException($"Unknown audit category \"{category}\".", nameof(category));
        }

        return _repository.InsertAsync(new AuditEntryDto
        {
            Id = "au" + Guid.NewGuid().ToString("n"),
            ActorId = actorId,
            Action = action,
            Target = target,
            Category = category
        }, cancellationToken);
    }
}
