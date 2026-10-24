namespace TopSupport.Models.Tickets;

/// <summary>Input to ITicketService.AddUserReplyAsync, mirroring ACT['user-reply'] in topsupport-app.js.</summary>
public class AddUserReplyRequest
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Sanitized rich-text HTML (equivalent to the demo's cleanHTML(F.html)).</summary>
    public string Html { get; set; } = string.Empty;
}

/// <summary>
/// Input to ITicketService.AddAgentReplyAsync, mirroring ACT['agent-reply']: a public reply
/// or an internal note, optionally changing status quietly afterward (the reply box's
/// "status after sending" picker).
/// </summary>
public class AddAgentReplyRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;

    /// <summary>True for an internal note (never shown to the end user); false for a public reply.</summary>
    public bool IsNote { get; set; }

    /// <summary>Optional status to apply quietly after a public reply is sent. Ignored for notes.</summary>
    public string? StatusAfterSending { get; set; }
}
