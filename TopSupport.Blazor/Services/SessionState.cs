using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TopSupport.Models.Enums;

namespace TopSupport.Blazor.Services;

/// <summary>
/// The demo's S.acct/S.role, now read from the signed-in account: the auth cookie set by the
/// sign-in page (see SignIn.razor and AuthCookie) carries the account id, name, role and the
/// Api bearer token. Scoped per circuit (or per request while prerendering / for endpoints).
/// </summary>
public class SessionState
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? _user;
    private string? _displayName;

    public SessionState(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public event Action? Changed;

    /// <summary>Null when signed out.</summary>
    public AppRole? Role => Enum.TryParse<AppRole>(User.FindFirstValue(AuthCookie.RoleClaim), out var role) ? role : null;

    /// <summary>The signed-in Users.Id ("u1") or Agents.Id ("a5"); empty when signed out.</summary>
    public string AccountId => User.FindFirstValue(AuthCookie.AccountIdClaim) ?? string.Empty;

    public string DisplayName => _displayName ?? User.FindFirstValue(AuthCookie.NameClaim) ?? string.Empty;

    /// <summary>The bearer token TopSupportApiClient sends to the Api; null when signed out.</summary>
    public string? ApiToken => User.FindFirstValue(AuthCookie.ApiTokenClaim);

    public bool IsSignedIn => Role is not null;

    /// <summary>After My Profile saves a new name, so the shell header (which listens to Changed) refreshes.</summary>
    public void SetDisplayName(string name)
    {
        _displayName = name;
        Changed?.Invoke();
    }

    /// <summary>Mirrors homeOf(role) in topsupport-app.js. Agent's home is the "open" queue --
    /// Queue.razor's route is "/queue/{QueueName}", so this must include the default queue
    /// name rather than just "/queue" (which 404s with no route match).</summary>
    public string HomeRoute() => Role switch
    {
        AppRole.User => "/my-tickets",
        AppRole.Agent => "/queue/open",
        AppRole.Admin => "/dashboard",
        _ => "/"
    };

    private ClaimsPrincipal User => _user ??= LoadUser();

    // Components get the user from the authentication state, which the server sets (already
    // completed) before rendering. Minimal API endpoints such as /account/signout have no
    // authentication state, so fall back to the request's user there.
    private ClaimsPrincipal LoadUser()
    {
        try
        {
            var task = _authenticationStateProvider.GetAuthenticationStateAsync();
            if (task.IsCompletedSuccessfully) return task.Result.User;
        }
        catch (InvalidOperationException)
        {
            // "GetAuthenticationStateAsync was called before SetAuthenticationState."
        }

        return _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    }
}
