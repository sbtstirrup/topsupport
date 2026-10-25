namespace TopSupport.Blazor.Services;

/// <summary>
/// The development-only test-account switcher: sign in as any seeded account (or switch between
/// them) without typing passwords. On only when the app runs in Development AND
/// "TestAccounts:Enabled" is true; the Api has its own matching switch
/// (Auth:TestAccountSwitcher:Enabled), so neither side alone can turn it on in Production.
/// </summary>
public sealed class TestAccountSwitcher
{
    public TestAccountSwitcher(IConfiguration configuration, IWebHostEnvironment environment)
    {
        IsEnabled = environment.IsDevelopment() && configuration.GetValue<bool>("TestAccounts:Enabled");
    }

    public bool IsEnabled { get; }

    /// <summary>
    /// POST /account/test-switch: signs out of the current account (audited) and signs in as
    /// <paramref name="accountId"/>, then goes back to <paramref name="returnUrl"/>; the app
    /// redirects to the new role's home if that page isn't theirs.
    /// </summary>
    public static async Task<IResult> SwitchAsync(HttpContext context, TopSupportApiClient api, ILogger<TestAccountSwitcher> logger, string? accountId, string? returnUrl)
    {
        var (account, error) = await api.TestSignInAsync(accountId ?? string.Empty);
        if (account is null)
        {
            return Results.LocalRedirect("~/signin?switchError=" + Uri.EscapeDataString(error ?? "That account can't sign in."));
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await api.SignOutAsync();
            }
            catch (ApiSessionExpiredException)
            {
                // The old token was already refused, so there's no session left to audit.
            }
            catch (HttpRequestException ex)
            {
                // Switching matters more than auditing the old account's sign-out, but the miss is logged.
                logger.LogWarning(ex, "Couldn't audit the sign-out before switching test account");
            }
        }

        await AuthCookie.SignInAsync(context, account);
        return Results.LocalRedirect(IsLocalPath(returnUrl) ? "~" + returnUrl : "~/home");
    }

    /// <summary>Only a local path: "/x" but not "//host" or "/\host".</summary>
    private static bool IsLocalPath(string? url)
        => url is { Length: > 0 } && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
