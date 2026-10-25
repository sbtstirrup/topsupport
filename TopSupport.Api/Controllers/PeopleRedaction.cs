using System.Security.Claims;
using TopSupport.Api.Auth;
using TopSupport.Models.People;

namespace TopSupport.Api.Controllers;

/// <summary>
/// What an end user may see of other people. The end-user pages (Raise, My Tickets, ticket view)
/// load the people lists only to show names, so contact details, usernames and account state are
/// removed for them; their own record stays complete. Agents get the lists unchanged.
/// </summary>
internal static class PeopleRedaction
{
    public static IReadOnlyList<UserDto> ForCaller(this ClaimsPrincipal user, IReadOnlyList<UserDto> users)
    {
        if (!user.IsEndUser()) return users;

        var self = user.AccountId();
        return users.Select(u => u.Id == self ? u : new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            OrgId = u.OrgId,
            Office = u.Office,
            Department = u.Department,
            Deleted = u.Deleted
        }).ToList();
    }

    public static IReadOnlyList<AgentDto> ForCaller(this ClaimsPrincipal user, IReadOnlyList<AgentDto> agents)
        => !user.IsEndUser() ? agents
            : agents.Select(a => new AgentDto { Id = a.Id, Name = a.Name, JobTitle = a.JobTitle, Active = a.Active }).ToList();

    public static IReadOnlyList<TeamDto> ForCaller(this ClaimsPrincipal user, IReadOnlyList<TeamDto> teams)
        => !user.IsEndUser() ? teams
            : teams.Select(t => new TeamDto { Id = t.Id, Name = t.Name, Department = t.Department }).ToList();
}
