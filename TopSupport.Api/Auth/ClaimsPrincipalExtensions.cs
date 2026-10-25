using System.Security.Claims;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;

namespace TopSupport.Api.Auth;

/// <summary>
/// The signed-in account, from the bearer token's claims. Controllers use these instead of any
/// actor/account id in a request body or query string, which the caller could set to anyone.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The Users.Id or Agents.Id the token was issued to.</summary>
    public static string AccountId(this ClaimsPrincipal user)
        => user.FindFirstValue(JwtTokenService.AccountIdClaim)
            ?? throw new InvalidOperationException("The request has no signed-in account.");

    public static AppRole AppRole(this ClaimsPrincipal user)
        => Enum.Parse<AppRole>(user.FindFirstValue(JwtTokenService.RoleClaim)
            ?? throw new InvalidOperationException("The request has no signed-in account."));

    public static bool IsEndUser(this ClaimsPrincipal user) => user.AppRole() == Models.Enums.AppRole.User;

    /// <summary>The ticket timeline/audit actor for the signed-in account ("user" or "agent").</summary>
    public static TicketActorRef Actor(this ClaimsPrincipal user)
        => new() { Type = user.IsEndUser() ? "user" : "agent", Id = user.AccountId() };
}
