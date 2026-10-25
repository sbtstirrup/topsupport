using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TopSupport.Blazor.Services;

/// <summary>
/// The Blazor app's auth cookie: who signed in plus the Api bearer token from
/// POST api/auth/sign-in. The cookie is encrypted by ASP.NET Core Data Protection and HttpOnly,
/// so the token never reaches browser script. It expires with the token (the cookie handler rejects
/// it after ExpiresUtc).
/// </summary>
public static class AuthCookie
{
    public const string AccountIdClaim = ClaimTypes.NameIdentifier;
    public const string NameClaim = ClaimTypes.Name;
    public const string RoleClaim = ClaimTypes.Role;
    public const string ApiTokenClaim = "topsupport:api_token";

    public static async Task SignInAsync(HttpContext httpContext, SignInResponse signIn)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(AccountIdClaim, signIn.AccountId),
            new Claim(NameClaim, signIn.DisplayName),
            new Claim(RoleClaim, signIn.Role),
            new Claim(ApiTokenClaim, signIn.Token)
        }, CookieAuthenticationDefaults.AuthenticationScheme, NameClaim, RoleClaim);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = signIn.ExpiresAt, IsPersistent = false, AllowRefresh = false });
    }
}
