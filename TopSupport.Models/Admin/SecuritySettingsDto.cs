namespace TopSupport.Models.Admin;

/// <summary>
/// Mirrors the demo's DB.security (buildSeed() in topsupport-app.js). Defaults are the seed values.
/// PAGES.security only shows the password rules; the session/lockout/2FA settings are stored and
/// round-tripped but have no editor, exactly as in the demo.
/// </summary>
public class SecuritySettingsDto
{
    public int MinLength { get; set; } = 10;
    public bool RequireUpper { get; set; } = true;
    public bool RequireLower { get; set; } = true;
    public bool RequireNumber { get; set; } = true;
    public bool RequireSymbol { get; set; } = true;
    public int ExpiryDays { get; set; } = 90;
    public int History { get; set; } = 5;
    public int SessionTimeout { get; set; } = 30;
    public int LockoutAttempts { get; set; } = 5;
    public string TwoFactorAgents { get; set; } = "Required";
    public string TwoFactorUsers { get; set; } = "Optional";

    /// <summary>
    /// Every setting as (demo key, String(value)) in the demo's Object.keys(DB.security) order.
    /// Used for storage (dbo.AppSettings "security.&lt;key&gt;"), change detection and the audit
    /// wording 'Updated security setting "&lt;key&gt;" to &lt;value&gt;' -- booleans render as
    /// "true"/"false" like JavaScript's String(v).
    /// </summary>
    public IReadOnlyList<(string Key, string Value)> ToKeyValues() => new[]
    {
        ("minLength", MinLength.ToString()),
        ("requireUpper", Js(RequireUpper)),
        ("requireLower", Js(RequireLower)),
        ("requireNumber", Js(RequireNumber)),
        ("requireSymbol", Js(RequireSymbol)),
        ("expiryDays", ExpiryDays.ToString()),
        ("history", History.ToString()),
        ("sessionTimeout", SessionTimeout.ToString()),
        ("lockoutAttempts", LockoutAttempts.ToString()),
        ("twoFactorAgents", TwoFactorAgents),
        ("twoFactorUsers", TwoFactorUsers)
    };

    /// <summary>Inverse of <see cref="ToKeyValues"/>; missing or unparseable keys keep the seed default.</summary>
    public static SecuritySettingsDto FromKeyValues(IReadOnlyDictionary<string, string> values)
    {
        var s = new SecuritySettingsDto();
        int Int(string k, int d) => values.TryGetValue(k, out var v) && int.TryParse(v, out var i) ? i : d;
        bool Bool(string k, bool d) => values.TryGetValue(k, out var v) ? v == "true" : d;
        string Str(string k, string d) => values.TryGetValue(k, out var v) ? v : d;

        s.MinLength = Int("minLength", s.MinLength);
        s.RequireUpper = Bool("requireUpper", s.RequireUpper);
        s.RequireLower = Bool("requireLower", s.RequireLower);
        s.RequireNumber = Bool("requireNumber", s.RequireNumber);
        s.RequireSymbol = Bool("requireSymbol", s.RequireSymbol);
        s.ExpiryDays = Int("expiryDays", s.ExpiryDays);
        s.History = Int("history", s.History);
        s.SessionTimeout = Int("sessionTimeout", s.SessionTimeout);
        s.LockoutAttempts = Int("lockoutAttempts", s.LockoutAttempts);
        s.TwoFactorAgents = Str("twoFactorAgents", s.TwoFactorAgents);
        s.TwoFactorUsers = Str("twoFactorUsers", s.TwoFactorUsers);
        return s;
    }

    public SecuritySettingsDto Clone() => (SecuritySettingsDto)MemberwiseClone();

    private static string Js(bool b) => b ? "true" : "false";
}

/// <summary>How many settings changed (the "(N changed)" in the demo's toast).</summary>
public class SaveSecuritySettingsResult
{
    public int Changed { get; set; }
}
