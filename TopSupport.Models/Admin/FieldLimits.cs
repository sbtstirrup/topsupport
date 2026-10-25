using System.Text.RegularExpressions;

namespace TopSupport.Models.Admin;

/// <summary>
/// Maximum lengths for admin form fields, shared by the Blazor inputs (maxlength) and the
/// services (authoritative). Each matches, or sits under, its dbo column / proc parameter size --
/// SQL Server silently truncates an over-long proc parameter, so the services must reject first.
/// </summary>
public static partial class FieldLimits
{
    public const int PersonName = 200;
    public const int Email = 256;
    public const int Phone = 50;
    public const int Office = 200;
    public const int JobTitle = 150;
    public const int OrgName = 200;
    public const int OrgAddress = 400;
    public const int OrgNote = 400;
    public const int TeamName = 200;
    public const int KbTitle = 200;
    public const int KbBody = 50_000;
    public const int SiteHeading = 200;
    public const int SiteBody = 2_000;
    public const int CannedTitle = 200;
    public const int SearchText = 200;
    public const int TaskText = 300;
    public const int TicketSubject = 300;
    public const int TopicText = 500;
    public const int TopicTextArea = 5_000;

    /// <summary>Caps a ticket's serialized per-topic answers (dbo.Tickets.FieldsJson is NVARCHAR(MAX)).</summary>
    public const int TopicFieldsJson = 16_000;

    /// <summary>Caps the PBKDF2 input so a huge password can't be used to burn CPU.</summary>
    public const int Password = 128;

    /// <summary>"Use N characters or fewer." when <paramref name="value"/> is longer than <paramref name="max"/>, else null.</summary>
    public static string? TooLong(string? value, int max)
        => value is not null && value.Trim().Length > max ? $"Use {max:N0} characters or fewer." : null;

    /// <summary>Optional phone: empty, or digits, spaces, + ( ) . - and "ext" (matching the seeded "ext. 4401" and the "—" placeholder phone of the Remote org).</summary>
    public static bool IsValidPhone(string? phone)
        => string.IsNullOrWhiteSpace(phone) || PhonePattern().IsMatch(phone.Trim());

    /// <summary>The demo's email check (/^[^@\s]+@[^@\s]+\.[^@\s]+$/). Check the length against <see cref="Email"/> first.</summary>
    public static bool IsValidEmail(string? email) => email is not null && EmailPattern().IsMatch(email.Trim());

    /// <summary>The demo's username check: 3 to 20 lower-case letters.</summary>
    public static bool IsValidUsername(string? username) => username is not null && UsernamePattern().IsMatch(username);

    [GeneratedRegex(@"^[0-9+()\s.ext—-]{1,50}$", RegexOptions.IgnoreCase)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex("^[a-z]{3,20}$")]
    private static partial Regex UsernamePattern();
}
