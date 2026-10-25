using Microsoft.Extensions.Time.Testing;
using TopSupport.Models.Enums;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Services.Admin;
using TopSupport.Services.Tests.Fakes;
using TopSupport.Services.Tickets;

namespace TopSupport.Services.Tests;

public class TicketStatusChangeTests
{
    private static readonly TicketActorRef Nadia = new() { Type = TicketActorTypes.Agent, Id = "a5" };

    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeAuditRepository _audit = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 25, 10, 0, 0, TimeSpan.Zero));
    private readonly TicketService _service;

    public TicketStatusChangeTests()
    {
        var users = new FakeUserRepository();
        var agents = new FakeAgentRepository();
        agents.Agents.Add(new AgentDto { Id = "a5", Name = "Nadia Hussain" });

        _service = new TicketService(
            _tickets, new FakeTeamRepository(), agents, users,
            new TicketFormValidator(users, new TicketFieldOptionsResolver(users, new FakeAssetRepository())),
            new AuditService(_audit), _time);
    }

    private TicketDto AddTicket(TicketStatus status)
    {
        var ticket = new TicketDto { Id = "T060001", Number = "060001", Status = status, Created = _time.GetUtcNow().AddDays(-1) };
        _tickets.Tickets[ticket.Id] = ticket;
        return ticket;
    }

    private Task<TicketDto> Change(string status) => _service.ChangeStatusAsync("T060001", status, Nadia, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Status_change_is_stored_written_to_the_timeline_and_audited()
    {
        AddTicket(TicketStatus.Open);

        var ticket = await Change("InProgress");

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(("T060001", "InProgress"), Assert.Single(_tickets.StatusUpdates));
        var entry = Assert.Single(ticket.Timeline);
        Assert.Equal("Status changed from Open to In Progress by N. Hussain", entry.Text);
        Assert.Equal(_time.GetUtcNow(), entry.At);
        var audit = Assert.Single(_audit.Entries);
        Assert.Equal(("a5", "Changed status to In Progress", "#060001"), (audit.ActorId, audit.Action, audit.Target));
    }

    [Theory]
    [InlineData("Resolved", "Closed by N. Hussain with status Resolved")]
    [InlineData("Closed", "Closed by N. Hussain with status Closed")]
    public async Task Resolving_or_closing_reads_as_closed(string status, string text)
    {
        AddTicket(TicketStatus.InProgress);

        var ticket = await Change(status);

        Assert.Equal(text, Assert.Single(ticket.Timeline).Text);
    }

    [Fact]
    public async Task Moving_out_of_resolved_reads_as_reopened()
    {
        AddTicket(TicketStatus.Resolved);

        var ticket = await Change("Open");

        Assert.Equal("Reopened by N. Hussain", Assert.Single(ticket.Timeline).Text);
    }

    [Fact]
    public async Task Same_status_changes_nothing()
    {
        AddTicket(TicketStatus.AwaitingUser);

        var ticket = await Change("AwaitingUser");

        Assert.Empty(ticket.Timeline);
        Assert.Empty(_tickets.StatusUpdates);
        Assert.Empty(_audit.Entries);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("1")]
    [InlineData("in progress")]
    public async Task Unknown_status_is_a_validation_error(string status)
    {
        AddTicket(TicketStatus.Open);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Change(status));

        Assert.Equal($"Unknown status '{status}'.", ex.Errors["status"]);
        Assert.Empty(_tickets.StatusUpdates);
    }

    [Fact]
    public async Task Unknown_ticket_is_not_found()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Change("Open"));
    }
}
