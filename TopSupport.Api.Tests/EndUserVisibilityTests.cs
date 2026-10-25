using System.Security.Claims;
using TopSupport.Api.Auth;
using TopSupport.Api.Controllers;
using TopSupport.Models.Enums;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;

namespace TopSupport.Api.Tests;

/// <summary>What an end user may see of tickets (EndUserTicketAccess) and of other people (PeopleRedaction).</summary>
public class EndUserVisibilityTests
{
    private static ClaimsPrincipal Signed(string accountId, AppRole role) => new(new ClaimsIdentity(new[]
    {
        new Claim(JwtTokenService.AccountIdClaim, accountId),
        new Claim(JwtTokenService.RoleClaim, role.ToString())
    }, "test"));

    private static readonly ClaimsPrincipal Owen = Signed("u11", AppRole.User);
    private static readonly ClaimsPrincipal Ruth = Signed("u12", AppRole.User);
    private static readonly ClaimsPrincipal Nadia = Signed("a5", AppRole.Agent);

    private static TicketActorRef User(string id) => new() { Type = TicketActorTypes.User, Id = id };
    private static TicketActorRef Agent(string id) => new() { Type = TicketActorTypes.Agent, Id = id };

    private static TicketTimelineEntryDto Entry(string id, TicketTimelineEntryKind kind, TicketActorRef by, int minute)
        => new() { Id = id, Kind = kind, By = by, At = new DateTimeOffset(2026, 10, 25, 9, minute, 0, TimeSpan.Zero) };

    private static TicketAttachmentDto File(string id, TicketActorRef by, string? entryId)
        => new() { Id = id, UploadedByType = by.Type, UploadedById = by.Id, TimelineEntryId = entryId };

    /// <summary>Owen's ticket: his reply and file, an agent reply with a file, an internal note with a file, and an unanchored agent file.</summary>
    private static TicketDto OwensTicket(TicketActorRef? createdBy = null) => new()
    {
        Id = "T1",
        RequesterUserId = "u11",
        AffectedUserId = "u11",
        CreatedBy = createdBy ?? User("u11"),
        Timeline =
        {
            Entry("created", TicketTimelineEntryKind.Event, createdBy ?? User("u11"), 0),
            Entry("user-reply", TicketTimelineEntryKind.Reply, User("u11"), 1),
            Entry("agent-reply", TicketTimelineEntryKind.Reply, Agent("a5"), 2),
            Entry("note", TicketTimelineEntryKind.Note, Agent("a5"), 3)
        },
        Attachments =
        {
            File("f-user", User("u11"), "user-reply"),
            File("f-agent-reply", Agent("a5"), "agent-reply"),
            File("f-note", Agent("a5"), "note"),
            File("f-agent-loose", Agent("a5"), null)
        },
        Tasks = { new TicketTaskDto { Id = "task1", Text = "Recover laptop" } }
    };

    [Fact]
    public void A_ticket_is_the_users_own_when_they_are_requester_or_affected()
    {
        var ticket = new TicketDto { RequesterUserId = "u11", AffectedUserId = "u12" };

        Assert.True(Owen.IsOwnTicket(ticket));
        Assert.True(Ruth.IsOwnTicket(ticket));
        Assert.False(Signed("u1", AppRole.User).IsOwnTicket(ticket));
    }

    [Fact]
    public void End_user_never_sees_notes_tasks_or_agent_files_off_public_replies()
    {
        var ticket = Owen.ForCaller(OwensTicket());

        Assert.DoesNotContain(ticket.Timeline, e => e.Kind == TicketTimelineEntryKind.Note);
        Assert.Empty(ticket.Tasks);
        Assert.Equal(new[] { "f-user", "f-agent-reply" }, ticket.Attachments.Select(a => a.Id));
    }

    [Fact]
    public void Agent_sees_the_whole_ticket()
    {
        var ticket = Nadia.ForCaller(OwensTicket());

        Assert.Equal(4, ticket.Timeline.Count);
        Assert.Equal(4, ticket.Attachments.Count);
        Assert.Single(ticket.Tasks);
    }

    [Fact]
    public void Agent_file_on_the_creation_event_is_visible_when_the_agent_raised_the_ticket()
    {
        var ticket = OwensTicket(createdBy: Agent("a5"));
        ticket.Attachments.Add(File("f-created", Agent("a5"), "created"));

        Assert.Contains(Owen.ForCaller(ticket).Attachments, a => a.Id == "f-created");
    }

    [Fact]
    public void Agent_file_on_a_user_created_event_stays_hidden()
    {
        var ticket = OwensTicket();
        ticket.Attachments.Add(File("f-created", Agent("a5"), "created"));

        Assert.DoesNotContain(Owen.ForCaller(ticket).Attachments, a => a.Id == "f-created");
    }

    [Theory]
    [InlineData("f-user", true)]
    [InlineData("f-agent-reply", true)]
    [InlineData("f-note", false)]
    [InlineData("f-agent-loose", false)]
    [InlineData("no-such-file", false)]
    public void Attachment_download_follows_the_same_rules(string attachmentId, bool visible)
    {
        Assert.Equal(visible, Owen.CanSeeAttachment(OwensTicket(), attachmentId));
    }

    [Fact]
    public void Another_user_cannot_download_files_from_a_ticket_that_is_not_theirs()
    {
        Assert.False(Ruth.CanSeeAttachment(OwensTicket(), "f-user"));
        Assert.True(Nadia.CanSeeAttachment(OwensTicket(), "f-note"));
    }

    [Fact]
    public void End_user_sees_other_people_by_name_only_and_themselves_in_full()
    {
        var users = new[]
        {
            new UserDto { Id = "u11", Name = "Owen Pryce", Email = "owen@x", Username = "opryce", Phone = "1", Locked = true },
            new UserDto { Id = "u12", Name = "Ruth Adeyemi", Email = "ruth@x", Username = "radeyemi", Phone = "2", Locked = true, Office = "Leeds" }
        };

        var seen = Owen.ForCaller(users);

        Assert.Same(users[0], seen[0]);
        var ruth = seen[1];
        Assert.Equal(("Ruth Adeyemi", "Leeds"), (ruth.Name, ruth.Office));
        Assert.Equal((string.Empty, string.Empty, null, false), (ruth.Email, ruth.Username, ruth.Phone, ruth.Locked));
    }

    [Fact]
    public void End_user_sees_agents_and_teams_without_contact_details_or_members()
    {
        var agents = Owen.ForCaller(new[] { new AgentDto { Id = "a5", Name = "Nadia Hussain", Email = "nadia@x", Phone = "4400", Username = "nhussain" } });
        var teams = Owen.ForCaller(new[] { new TeamDto { Id = "t1", Name = "Service Desk", LeadAgentId = "a3", MemberAgentIds = { "a1", "a3" } } });

        Assert.Equal(("Nadia Hussain", string.Empty, null, string.Empty), (agents[0].Name, agents[0].Email, agents[0].Phone, agents[0].Username));
        Assert.Equal(("Service Desk", string.Empty), (teams[0].Name, teams[0].LeadAgentId));
        Assert.Empty(teams[0].MemberAgentIds);
    }

    [Fact]
    public void Agents_get_people_lists_unchanged()
    {
        var users = new[] { new UserDto { Id = "u12", Email = "ruth@x" } };

        Assert.Same(users, Nadia.ForCaller(users));
    }
}
