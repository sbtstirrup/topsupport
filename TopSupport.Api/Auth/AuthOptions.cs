namespace TopSupport.Api.Auth;

/// <summary>
/// The "Auth" configuration section: how the Api signs and checks the bearer tokens it issues
/// at sign-in. SigningKey must be at least 32 bytes and kept out of source control outside
/// development (user secrets, an environment variable or a key vault).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "TopSupport.Api";
    public string Audience { get; set; } = "TopSupport.Blazor";
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>A working day; the Blazor app's cookie expires with the token.</summary>
    public int TokenLifetimeMinutes { get; set; } = 480;

    /// <summary>
    /// The test-account switcher (sign in as anyone without a password). Only ever active when the
    /// Api runs in Development AND this is true; appsettings.Development.json turns it on.
    /// </summary>
    public TestAccountSwitcherOptions TestAccountSwitcher { get; set; } = new();

    public sealed class TestAccountSwitcherOptions
    {
        public bool Enabled { get; set; }
    }
}
