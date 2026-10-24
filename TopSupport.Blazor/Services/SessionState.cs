using TopSupport.Models.Enums;

namespace TopSupport.Blazor.Services;

/// <summary>
/// Minimal stand-in for the demo's S.acct/S.role globals. This skeleton pass has no real
/// sign-in flow yet (see TopSupport.Api/Identity) -- a role-switcher control sets this
/// directly so nav/route gating is visibly testable. Scoped per-circuit, which for Blazor
/// Server is equivalent to the demo's single in-memory S object for that user's session.
/// </summary>
public class SessionState
{
    public AppRole? Role { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// The signed-in account's id in TopSupport.Database: a Users.Id ("u1") for role == User,
    /// or an Agents.Id ("a1"/"a5") for role == Agent/Admin. Mirrors setRole(role)'s S.acct
    /// assignment in topsupport-app.js exactly (user -> u1, agent -> a1, admin -> a5) -- the
    /// role switcher is a demo stand-in for real sign-in, so every role maps to one fixed
    /// account. Empty when signed out.
    /// </summary>
    public string AccountId { get; private set; } = string.Empty;

    public event Action? Changed;

    public void SetRole(AppRole? role)
    {
        Role = role;
        (DisplayName, AccountId) = role switch
        {
            AppRole.User => ("Amira Patel", "u1"),
            AppRole.Agent => ("Jamie Moore", "a1"),
            AppRole.Admin => ("Nadia Hussain", "a5"),
            _ => (string.Empty, string.Empty)
        };
        Changed?.Invoke();
    }

    /// <summary>After My Profile saves a new name, so the shell header (which listens to Changed) refreshes.</summary>
    public void SetDisplayName(string name)
    {
        DisplayName = name;
        Changed?.Invoke();
    }

    public bool IsSignedIn => Role is not null;

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
}
