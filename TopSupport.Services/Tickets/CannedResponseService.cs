using System.Text.RegularExpressions;
using TopSupport.Models.Admin;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Canned responses: CRUD validated as ACT['cn-save'] does, and FillPlaceholdersAsync, an exact port of
/// fillPlaceholders(text, t) in topsupport-app.js (~line 1546-1551): a {token} -> value map
/// built from the ticket/requester/acting-agent, substituted with a regex that leaves any
/// unrecognised {token} untouched (m0 fallback in the demo).
/// </summary>
public partial class CannedResponseService
{
    private readonly ICannedResponseRepository _repository;
    private readonly IUserRepository _users;
    private readonly IAgentRepository _agents;
    private readonly AuditService _audit;
    private readonly TimeProvider _time;

    public CannedResponseService(ICannedResponseRepository repository, IUserRepository users, IAgentRepository agents, AuditService audit, TimeProvider time)
    {
        _time = time;
        _repository = repository;
        _users = users;
        _agents = agents;
        _audit = audit;
    }

    public Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <summary>
    /// Creates a response with a server-generated id (any client-supplied Id is ignored). Throws
    /// <see cref="ValidationException"/> on invalid input.
    /// </summary>
    public async Task<CannedResponseDto> CreateAsync(CannedResponseDto response, string? actorId = null, CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        Validate(response, null, all);

        // Demo: uid('c') for a new response -- the id is always ours, never the caller's.
        var created = await _repository.InsertAsync(new CannedResponseDto
        {
            Id = "c" + Guid.NewGuid().ToString("n"),
            Title = response.Title.Trim(),
            Body = response.Body,
            SortOrder = response.SortOrder
        }, cancellationToken);
        await _audit.LogAsync(actorId ?? TicketActorTypes.System, "Created canned response", created.Title, AuditCategories.Ticket, cancellationToken);
        return created;
    }

    /// <summary>
    /// Returns false (nothing written or audited) when there's no such id. Throws
    /// <see cref="ValidationException"/> on invalid input.
    /// </summary>
    public async Task<bool> UpdateAsync(CannedResponseDto response, string? actorId = null, CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        if (!all.Any(c => c.Id == response.Id)) return false;
        Validate(response, response.Id, all);

        // Title trimmed, body as typed -- as ACT['cn-save'] stores them.
        response.Title = response.Title.Trim();
        await _repository.UpdateAsync(response, cancellationToken);
        await _audit.LogAsync(actorId ?? TicketActorTypes.System, "Edited canned response", response.Title, AuditCategories.Ticket, cancellationToken);
        return true;
    }

    /// <summary>Returns false (nothing written or audited) when there's no such id.</summary>
    public async Task<bool> DeleteAsync(string id, string? actorId = null, CancellationToken cancellationToken = default)
    {
        // Look the title up first: it's the audit target, and the row is gone afterwards.
        var existing = (await _repository.GetAllAsync(cancellationToken)).FirstOrDefault(c => c.Id == id);
        if (existing is null) return false;
        await _repository.DeleteAsync(id, cancellationToken);
        await _audit.LogAsync(actorId ?? TicketActorTypes.System, "Deleted canned response", existing.Title, AuditCategories.Ticket, cancellationToken);
        return true;
    }

    // Body is NVARCHAR(MAX): capped at the knowledge base article size so it can't grow unbounded.
    private const int MaxBodyLength = FieldLimits.KbBody;

    /// <summary>
    /// The authoritative half of ACT['cn-save']'s checks, with the same messages as Canned.razor
    /// (which also checks placeholders for instant feedback). <paramref name="selfId"/> is the
    /// response being edited, excluded from the unique-title check; null when creating.
    /// </summary>
    private static void Validate(CannedResponseDto response, string? selfId, IReadOnlyList<CannedResponseDto> all)
    {
        var errors = new Dictionary<string, string>();
        var title = (response.Title ?? string.Empty).Trim();
        if (title.Length == 0) errors["title"] = "Enter a title.";
        else if (FieldLimits.TooLong(title, FieldLimits.CannedTitle) is { } titleTooLong) errors["title"] = titleTooLong;
        else if (all.Any(c => c.Id != selfId && string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase))) errors["title"] = "Another response already has this title.";

        if (string.IsNullOrWhiteSpace(response.Body)) errors["body"] = "Enter the message.";
        else if (FieldLimits.TooLong(response.Body, MaxBodyLength) is { } bodyTooLong) errors["body"] = bodyTooLong;

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    /// <summary>
    /// Substitutes the 11 {ticket.*}/{recipient.*}/{agent.*} tokens in <paramref name="body"/>.
    /// When <paramref name="ticket"/> is null, uses the demo's exact fallback sample values
    /// (ticket #060012, requester "Amira Patel", etc.) so a preview still renders something
    /// sensible before a real ticket exists.
    /// </summary>
    public async Task<string> FillPlaceholdersAsync(string body, TicketDto? ticket, string agentId, CancellationToken cancellationToken = default)
    {
        // The demo's sample values are only for a preview with no ticket (fillPlaceholders(text, null)).
        // With a real ticket, a missing requester is an error -- never a sample person in a real email.
        string recipientName = "Amira Patel";
        string recipientEmail = "amira.patel@topcashback.example";
        string ticketNumber = "#060012";
        string ticketSubject = "Laptop will not charge";
        string ticketStatus = "Open";
        string ticketPriority = "Normal";
        string slaDueText = FormatDateTime(_time.GetUtcNow());

        if (ticket != null)
        {
            var requester = await _users.GetByIdAsync(ticket.RequesterUserId, cancellationToken)
                ?? throw new InvalidOperationException($"Ticket '{ticket.Id}' has requester '{ticket.RequesterUserId}', who doesn't exist.");
            recipientName = requester.Name;
            recipientEmail = requester.Email;
            ticketNumber = "#" + ticket.Number;
            ticketSubject = ticket.Subject;
            ticketStatus = TicketStatusText.Label(ticket.Status);
            ticketPriority = ticket.Priority.ToString();
            slaDueText = FormatDateTime(TicketSla.SlaDue(ticket));
        }

        // The signed-in agent always exists; if not, that's a bug to surface, not paper over.
        var agent = await _agents.GetByIdAsync(agentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent '{agentId}' filling a canned response doesn't exist.");
        var agentName = agent.Name;
        var agentPhone = agent.Phone ?? string.Empty;

        var map = new Dictionary<string, string>
        {
            ["ticket.number"] = ticketNumber,
            ["ticket.subject"] = ticketSubject,
            ["ticket.status"] = ticketStatus,
            ["ticket.priority"] = ticketPriority,
            ["ticket.sla_due"] = slaDueText,
            ["recipient.name"] = recipientName,
            ["recipient.first"] = recipientName.Split(' ')[0],
            ["recipient.email"] = recipientEmail,
            ["agent.name"] = agentName,
            ["agent.first"] = agentName.Split(' ')[0],
            ["agent.phone"] = agentPhone
        };

        return PlaceholderPattern().Replace(body, m =>
            map.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
    }

    private static string FormatDateTime(DateTimeOffset value) => value.ToString("dd/MM/yyyy, HH:mm");

    [GeneratedRegex(@"\{([a-z_.]+)\}")]
    private static partial Regex PlaceholderPattern();
}
