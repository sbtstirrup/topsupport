namespace TopSupport.Api.Auth;

/// <summary>
/// Authorization policy names, matching PAGES[name].roles in topsupport-app.js. Every endpoint
/// needs a signed-in account (the fallback policy) unless it is marked [AllowAnonymous].
/// </summary>
public static class Policies
{
    /// <summary>Service desk screens: agents and admins.</summary>
    public const string Agent = "Agent";

    /// <summary>Admin console screens.</summary>
    public const string Admin = "Admin";
}
