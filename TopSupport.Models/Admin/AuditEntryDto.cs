namespace TopSupport.Models.Admin;

/// <summary>Mirrors an entry in the demo's DB.audit (see audit(action, target, cat) in topsupport-app.js).</summary>
public class AuditEntryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset At { get; set; }

    /// <summary>Users.Id or Agents.Id of whoever did it (the demo's S.acct).</summary>
    public string ActorId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;

    /// <summary>One of <see cref="AuditCategories"/>.</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>The audit categories used by the demo (dbo.AuditEntries has a CHECK constraint on these).</summary>
public static class AuditCategories
{
    public const string SignIn = "Sign-in";
    public const string Ticket = "Ticket";
    public const string User = "User";
    public const string Organisation = "Organisation";
    public const string Permission = "Permission";

    public static readonly IReadOnlyList<string> All = new[] { SignIn, Ticket, User, Organisation, Permission };
}
