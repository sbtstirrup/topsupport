using System.Text.RegularExpressions;

namespace TopSupport.Models.Admin;

/// <summary>The signed-in account (the demo's me()) as shown on My Profile: a Users or Agents row.</summary>
public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsAgent { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }

    /// <summary>Users only.</summary>
    public string? Department { get; set; }

    /// <summary>Users only.</summary>
    public string? Office { get; set; }

    /// <summary>Agents only: their departments joined with ", " in DEPTS order.</summary>
    public string? Departments { get; set; }
}

/// <summary>ACT['pf-save'] payload. The account being edited is always the actor's own.</summary>
public class UpdateProfileRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

/// <summary>ACT['pf-pw'] payload: the actor changes their own password.</summary>
public class ChangePasswordRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Field key ("name", "email", "phone", "cur", "nw") -> message, as in the demo's F.errs / F.pwErrs. Empty means saved.</summary>
public class ProfileSaveResult
{
    public Dictionary<string, string> Errors { get; set; } = new();
}

/// <summary>
/// Validation shared by Profile.razor (instant feedback) and ProfileService (authoritative), so
/// both give the demo's exact messages.
/// </summary>
public static partial class ProfileRules
{
    /// <summary>Port of ACT['pf-save']'s checks.</summary>
    public static Dictionary<string, string> ValidateDetails(string? name, string? email, string? phone)
    {
        var e = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name)) e["name"] = "Enter your name.";
        else if (FieldLimits.TooLong(name, FieldLimits.PersonName) is { } nameTooLong) e["name"] = nameTooLong;

        // Length first, so an over-long address isn't run through the regex at all.
        if (FieldLimits.TooLong(email, FieldLimits.Email) is { } emailTooLong) e["email"] = emailTooLong;
        else if (!FieldLimits.IsValidEmail(email)) e["email"] = "Enter an email address like name@topcashback.example.";

        if (FieldLimits.TooLong(phone, FieldLimits.Phone) is { } phoneTooLong) e["phone"] = phoneTooLong;
        else if (!string.IsNullOrEmpty(phone) && !PhonePattern().IsMatch(phone)) e["phone"] = "Use digits, spaces and + only.";
        return e;
    }

    /// <summary>
    /// Port of pwRules(pw, cf, cur): (label, met) for each rule, driven by the security settings.
    /// <paramref name="differsFromCurrent"/> stands in for the demo's pw !== cur -- the server works
    /// it out by verifying the new password against the stored hash, since plaintext isn't kept.
    /// </summary>
    public static IReadOnlyList<(string Label, bool Met)> PasswordRules(SecuritySettingsDto s, string? pw, string? cf, bool differsFromCurrent)
    {
        pw ??= string.Empty;
        return new[]
        {
            ($"At least {s.MinLength} characters", pw.Length >= s.MinLength),
            ($"No more than {FieldLimits.Password} characters", pw.Length <= FieldLimits.Password),
            ("An uppercase letter", !s.RequireUpper || UpperPattern().IsMatch(pw)),
            ("A lowercase letter", !s.RequireLower || LowerPattern().IsMatch(pw)),
            ("A number", !s.RequireNumber || DigitPattern().IsMatch(pw)),
            ("A symbol such as ! or #", !s.RequireSymbol || SymbolPattern().IsMatch(pw)),
            ("Different from your current password", pw.Length > 0 && differsFromCurrent),
            ("Both new passwords match", pw.Length > 0 && pw == cf)
        };
    }

    [GeneratedRegex(@"^[0-9+()\s.ext-]{5,}$", RegexOptions.IgnoreCase)]
    private static partial Regex PhonePattern();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperPattern();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowerPattern();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitPattern();

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex SymbolPattern();
}
