using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TopSupport.Api.Auth;
using TopSupport.Api.Controllers;

namespace TopSupport.Api.Tests;

/// <summary>
/// Deny by default: every endpoint's access level is listed here, so a new endpoint fails this
/// test until someone decides who may call it, and an attribute that changes an existing
/// endpoint's access shows up as a failure rather than slipping through.
/// </summary>
public class EndpointAccessTests
{
    public enum Access { Anonymous, SignedIn, Agent, Admin }

    private static readonly Dictionary<string, Access> Expected = new()
    {
        ["AdminAgents.GetAgents"] = Access.Admin,
        ["AdminAgents.GetTeams"] = Access.Admin,
        ["AdminAgents.GetActiveTicketCounts"] = Access.Admin,
        ["AdminAgents.SaveAgent"] = Access.Admin,
        ["AdminAgents.ResetPassword"] = Access.Admin,
        ["AdminAgents.SaveTeam"] = Access.Admin,
        ["AdminAgents.CheckTeamDelete"] = Access.Admin,
        ["AdminAgents.DeleteTeam"] = Access.Admin,
        ["Attachments.GetContent"] = Access.SignedIn,
        ["Audit.GetRecent"] = Access.Admin,
        ["Auth.SignIn"] = Access.Anonymous,
        ["Auth.SignOut"] = Access.SignedIn,
        ["Auth.GetTestAccounts"] = Access.Anonymous,
        ["Auth.TestSignIn"] = Access.Anonymous,
        ["CannedResponses.GetAll"] = Access.Agent,
        ["CannedResponses.Create"] = Access.Agent,
        ["CannedResponses.Update"] = Access.Agent,
        ["CannedResponses.Delete"] = Access.Agent,
        ["CannedResponses.Preview"] = Access.Agent,
        ["Dashboard.Get"] = Access.Admin,
        ["Health.Get"] = Access.Anonymous,
        ["KnowledgeBase.GetAll"] = Access.Admin,
        ["KnowledgeBase.GetPopular"] = Access.Anonymous,
        ["KnowledgeBase.Create"] = Access.Admin,
        ["KnowledgeBase.Update"] = Access.Admin,
        ["KnowledgeBase.Delete"] = Access.Admin,
        ["Lookup.GetUsers"] = Access.Agent,
        ["Lookup.GetAgents"] = Access.SignedIn,
        ["Lookup.GetTeams"] = Access.SignedIn,
        ["Lookup.GetAssets"] = Access.SignedIn,
        ["Orgs.GetList"] = Access.Admin,
        ["Orgs.Get"] = Access.Admin,
        ["Orgs.GetOpenTickets"] = Access.Admin,
        ["Orgs.Create"] = Access.Admin,
        ["Orgs.Update"] = Access.Admin,
        ["Profile.Get"] = Access.SignedIn,
        ["Profile.UpdateDetails"] = Access.SignedIn,
        ["Profile.ChangePassword"] = Access.SignedIn,
        ["RolePermissions.GetAll"] = Access.Admin,
        ["RolePermissions.Set"] = Access.Admin,
        ["Settings.GetSecurity"] = Access.SignedIn,
        ["Settings.SaveSecurity"] = Access.Admin,
        ["Settings.GetSite"] = Access.Anonymous,
        ["Settings.SaveSite"] = Access.Admin,
        ["Tickets.GetById"] = Access.SignedIn,
        ["Tickets.GetByRequester"] = Access.SignedIn,
        ["Tickets.GetTopics"] = Access.SignedIn,
        ["Tickets.GetFieldSchema"] = Access.SignedIn,
        ["Tickets.Create"] = Access.SignedIn,
        ["Tickets.AddUserReply"] = Access.SignedIn,
        ["Tickets.AddAgentReply"] = Access.Agent,
        ["Tickets.ChangeStatus"] = Access.Agent,
        ["Tickets.ChangePriority"] = Access.Agent,
        ["Tickets.ChangeTopic"] = Access.Agent,
        ["Tickets.ChangeProperties"] = Access.Agent,
        ["Tickets.Assign"] = Access.Agent,
        ["Tickets.Claim"] = Access.Agent,
        ["Tickets.BulkStatus"] = Access.Agent,
        ["Tickets.BulkPriority"] = Access.Agent,
        ["Tickets.BulkAssign"] = Access.Agent,
        ["Tickets.BulkClose"] = Access.Agent,
        ["Tickets.AddTask"] = Access.Agent,
        ["Tickets.SetTaskDone"] = Access.Agent,
        ["Tickets.GetAttachments"] = Access.SignedIn,
        ["Tickets.UploadAttachment"] = Access.SignedIn,
        ["Tickets.Queue"] = Access.Agent,
        ["Tickets.NavCounts"] = Access.SignedIn,
        ["Users.GetList"] = Access.Admin,
        ["Users.GetAll"] = Access.SignedIn,
        ["Users.Get"] = Access.Admin,
        ["Users.Create"] = Access.Admin,
        ["Users.Update"] = Access.Admin,
        ["Users.ToggleLock"] = Access.Admin,
        ["Users.ResetPassword"] = Access.Admin,
        ["Users.Delete"] = Access.Admin,
        ["WorkerMonitor.GetJobs"] = Access.Admin,
        ["WorkerMonitor.GetNotifications"] = Access.Admin,
    };

    /// <summary>
    /// What the attributes grant, as ASP.NET Core applies them: [AllowAnonymous] on the action or
    /// controller skips authorization; otherwise every [Authorize] policy on both must pass;
    /// with none, the fallback policy (Program.cs) requires a signed-in account.
    /// </summary>
    private static Access Actual(MethodInfo action)
    {
        var controller = action.DeclaringType!;
        if (action.IsDefined(typeof(AllowAnonymousAttribute)) || controller.IsDefined(typeof(AllowAnonymousAttribute))) return Access.Anonymous;

        var policies = action.GetCustomAttributes<AuthorizeAttribute>().Concat(controller.GetCustomAttributes<AuthorizeAttribute>())
            .Select(a => a.Policy ?? throw new InvalidOperationException($"{Name(action)} has an [Authorize] without a policy."))
            .ToList();
        return policies.Contains(Policies.Admin) ? Access.Admin
            : policies.Contains(Policies.Agent) ? Access.Agent
            : policies.Count == 0 ? Access.SignedIn
            : throw new InvalidOperationException($"{Name(action)} uses an unknown policy: {string.Join(", ", policies)}.");
    }

    private static string Name(MethodInfo action) => action.DeclaringType!.Name.Replace("Controller", "") + "." + action.Name;

    private static readonly List<MethodInfo> Actions = typeof(TicketsController).Assembly.GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        .Where(m => m.IsDefined(typeof(HttpMethodAttribute), inherit: true))
        .ToList();

    [Fact]
    public void Every_endpoint_has_the_access_level_listed_for_it()
    {
        var actual = Actions.ToDictionary(Name, Actual);

        var unlisted = actual.Keys.Except(Expected.Keys).Order().ToList();
        var gone = Expected.Keys.Except(actual.Keys).Order().ToList();
        var changed = actual.Where(kv => Expected.TryGetValue(kv.Key, out var e) && e != kv.Value)
            .Select(kv => $"{kv.Key}: expected {Expected[kv.Key]}, is {kv.Value}").ToList();

        Assert.True(unlisted.Count == 0, "Endpoints with no listed access level: " + string.Join(", ", unlisted));
        Assert.True(gone.Count == 0, "Listed endpoints that no longer exist: " + string.Join(", ", gone));
        Assert.True(changed.Count == 0, "Endpoints whose access changed: " + string.Join("; ", changed));
    }

    [Fact]
    public void Only_sign_in_and_public_page_content_are_anonymous()
    {
        var anonymous = Actions.Where(a => Actual(a) == Access.Anonymous).Select(Name).Order();

        Assert.Equal(new[] { "Auth.GetTestAccounts", "Auth.SignIn", "Auth.TestSignIn", "Health.Get", "KnowledgeBase.GetPopular", "Settings.GetSite" }, anonymous);
    }
}
