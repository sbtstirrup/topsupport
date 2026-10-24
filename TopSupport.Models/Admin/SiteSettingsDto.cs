namespace TopSupport.Models.Admin;

/// <summary>Mirrors the demo's DB.site: text on the public landing and sign-in pages. Defaults are the buildSeed() values.</summary>
public class SiteSettingsDto
{
    public string LandingTitle { get; set; } = "Need help with IT?";
    public string LandingBody { get; set; } = "Raise a ticket and the TopCashback IT team will pick it up. You can follow progress, reply to the team and see every update in one place.";
    public string SigninTitle { get; set; } = "Sign in to TopSupport";
    public string SigninBody { get; set; } = "Use your TopCashback username and password.";
}

/// <summary>ACT['site-save'] payload. ActorId is the signed-in admin, for the audit trail.</summary>
public class SaveSiteSettingsRequest
{
    public string ActorId { get; set; } = string.Empty;
    public SiteSettingsDto Settings { get; set; } = new();
}
