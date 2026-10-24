using System.Text.RegularExpressions;
using TopSupport.Models.Tickets;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Tickets;

/// <summary>
/// Default ICannedResponseService. FillPlaceholdersAsync is an exact port of
/// fillPlaceholders(text, t) in topsupport-app.js (~line 1546-1551): a {token} -> value map
/// built from the ticket/requester/acting-agent, substituted with a regex that leaves any
/// unrecognised {token} untouched (m0 fallback in the demo).
/// </summary>
public partial class CannedResponseService : ICannedResponseService
{
    private readonly ICannedResponseRepository _repository;
    private readonly IUserRepository _users;
    private readonly IAgentRepository _agents;

    public CannedResponseService(ICannedResponseRepository repository, IUserRepository users, IAgentRepository agents)
    {
        _repository = repository;
        _users = users;
        _agents = agents;
    }

    public Task<IReadOnlyList<CannedResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<CannedResponseDto> CreateAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
        => _repository.InsertAsync(response, cancellationToken);

    public Task UpdateAsync(CannedResponseDto response, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(response, cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    public async Task<string> FillPlaceholdersAsync(string body, TicketDto? ticket, string? agentId, CancellationToken cancellationToken = default)
    {
        // Demo's fallback sample values when there's no ticket yet (fillPlaceholders(text, null)).
        string recipientName = "Amira Patel";
        string recipientEmail = "amira.patel@topcashback.example";
        string ticketNumber = "#060012";
        string ticketSubject = "Laptop will not charge";
        string ticketStatus = "Open";
        string ticketPriority = "Normal";
        string slaDueText = FormatDateTime(DateTimeOffset.UtcNow);

        if (ticket != null)
        {
            var requester = await _users.GetByIdAsync(ticket.RequesterUserId, cancellationToken);
            recipientName = requester?.Name ?? recipientName;
            recipientEmail = requester?.Email ?? recipientEmail;
            ticketNumber = "#" + ticket.Number;
            ticketSubject = ticket.Subject;
            ticketStatus = ticket.Status.ToString();
            ticketPriority = ticket.Priority.ToString();
            slaDueText = FormatDateTime(TicketSla.SlaDue(ticket));
        }

        string agentName = "Amira Patel";
        string agentPhone = string.Empty;
        if (!string.IsNullOrEmpty(agentId))
        {
            var agent = await _agents.GetByIdAsync(agentId, cancellationToken);
            if (agent != null)
            {
                agentName = agent.Name;
                agentPhone = agent.Phone ?? string.Empty;
            }
        }

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
