using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Admin Organisations screens: port of PAGES.orgs/PAGES.org and ACT['org-save'] in
/// topsupport-app.js. Throws <see cref="ValidationException"/> for form errors and
/// <see cref="KeyNotFoundException"/> when the organisation doesn't exist.
/// </summary>
public class OrgAdminService
{
    private readonly IOrgRepository _repository;
    private readonly ITicketRepository _tickets;
    private readonly AuditService _audit;

    public OrgAdminService(IOrgRepository repository, ITicketRepository tickets, AuditService audit)
    {
        _repository = repository;
        _tickets = tickets;
        _audit = audit;
    }

    public Task<IReadOnlyList<OrgListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
        => _repository.GetListAsync(cancellationToken);

    public Task<OrgDto?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <summary>Active tickets raised by or for a member, newest first (PAGES.org "Open Tickets").</summary>
    public async Task<IReadOnlyList<TicketDto>> GetOpenTicketsAsync(string id, CancellationToken cancellationToken = default)
    {
        var ids = await _repository.GetOpenTicketIdsAsync(id, cancellationToken);
        if (ids.Count == 0) return Array.Empty<TicketDto>();

        // The active-ticket set is small, so one queue read filtered here beats a read per id.
        var active = (await _tickets.GetActiveForQueueAsync(cancellationToken)).ToDictionary(t => t.Id);
        return ids.Where(active.ContainsKey).Select(ticketId => active[ticketId]).ToList();
    }

    public async Task<OrgDto> CreateAsync(SaveOrgRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(string.Empty, request, cancellationToken);

        var org = ToOrg("o" + Guid.NewGuid().ToString("n")[..12], request);
        await _repository.InsertAsync(org, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Created organisation", org.Name, AuditCategories.Organisation, cancellationToken);
        return org;
    }

    public async Task<OrgDto> UpdateAsync(string id, SaveOrgRequest request, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByIdAsync(id, cancellationToken) is null) throw new KeyNotFoundException($"Organisation {id} not found.");
        await ValidateAsync(id, request, cancellationToken);

        var org = ToOrg(id, request);
        await _repository.UpdateAsync(org, cancellationToken);
        await _audit.LogAsync(request.ActorId, "Edited organisation", org.Name, AuditCategories.Organisation, cancellationToken);
        return org;
    }

    /// <summary>ACT['org-save']'s checks, with the demo's exact messages.</summary>
    private async Task ValidateAsync(string id, SaveOrgRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();
        var name = request.Name.Trim();

        if (string.IsNullOrEmpty(name))
        {
            errors["name"] = "Enter a name.";
        }
        else if (FieldLimits.TooLong(name, FieldLimits.OrgName) is { } nameErr)
        {
            errors["name"] = nameErr;
        }
        else
        {
            var orgs = await _repository.GetListAsync(cancellationToken);
            if (orgs.Any(o => o.Id != id && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                errors["name"] = "An organisation with this name already exists.";
            }
        }

        if (string.IsNullOrWhiteSpace(request.Address)) errors["address"] = "Enter an address.";
        else if (FieldLimits.TooLong(request.Address, FieldLimits.OrgAddress) is { } addressErr) errors["address"] = addressErr;

        // Phone stays optional, as in the demo.
        if (FieldLimits.TooLong(request.Phone, FieldLimits.Phone) is { } phoneErr) errors["phone"] = phoneErr;
        else if (!FieldLimits.IsValidPhone(request.Phone)) errors["phone"] = "Use digits, spaces and + only.";

        if (FieldLimits.TooLong(request.Note, FieldLimits.OrgNote) is { } noteErr) errors["note"] = noteErr;

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static OrgDto ToOrg(string id, SaveOrgRequest request) => new()
    {
        Id = id,
        Name = request.Name.Trim(),
        Address = request.Address.Trim(),
        Phone = request.Phone.Trim(),
        Note = request.Note.Trim()
    };
}
