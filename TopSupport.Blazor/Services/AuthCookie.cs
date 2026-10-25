using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TopSupport.Blazor.Services;

/// <summary>
/// The Blazor app's auth cookie: who signed in plus the Api bearer token from
/// POST api/auth/sign-in. The cookie is encrypted by ASP.NET Core Data Protection and HttpOnly,
/// so the token never reaches browser script; it expires with the token.
/// </summary>
public static class AuthCookie
{
    public const string AccountIdClaim = ClaimTypes.NameIdentifier;
    public const string NameClaim = ClaimTypes.Name;
    public const string RoleClaim = ClaimTypes.Role;
    public const string ApiTokenClaim = "topsupport:api_token";
    public const string ApiTokenExpiresClaim = "topsupport:api_token_expires";

    public static async Task SignInAsync(HttpContext httpContext, SignInResponse signIn)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(AccountIdClaim, signIn.AccountId),
            new Claim(NameClaim, signIn.DisplayName),
            new Claim(RoleClaim, signIn.Role),
            new Claim(ApiTokenClaim, signIn.Token),
            new Claim(ApiTokenExpiresClaim, signIn.ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
        }, CookieAuthenticationDefaults.AuthenticationScheme, NameClaim, RoleClaim);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = signIn.ExpiresAt, IsPersistent = false, AllowRefresh = false });
    }

    /// <summary>Cookie OnValidatePrincipal: a cookie whose Api token has expired is signed out.</summary>
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var expires = context.Principal?.FindFirstValue(ApiTokenExpiresClaim);
        if (!long.TryParse(expires, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
