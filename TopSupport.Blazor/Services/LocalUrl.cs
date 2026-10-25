namespace TopSupport.Blazor.Services;

/// <summary>
/// Return URLs (sign-in's ?returnUrl, the test switcher's returnUrl) may only point back into this
/// app, or they're an open redirect. Same rules as ASP.NET Core's IsLocalUrl for a "/" path.
/// </summary>
public static class LocalUrl
{
    /// <summary>"/x" yes; "//host", "/\host", "https://host", a control character or "" no.</summary>
    public static bool IsLocal(string? url)
        => url is { Length: > 0 } && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            && !url.Any(char.IsControl);
}
