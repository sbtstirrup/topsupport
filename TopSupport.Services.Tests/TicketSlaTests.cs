using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;
using TopSupport.Services.Tickets;

namespace TopSupport.Services.Tests;

public class TicketSlaTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private static TicketDto Ticket(TicketPriority priority, TicketStatus status = TicketStatus.Open, DateTimeOffset? firstAgentReply = null, string? lastReplyBy = null)
        => new() { Id = "T1", Priority = priority, Status = status, Created = Created, FirstAgentReplyAt = firstAgentReply, LastReplyByType = lastReplyBy };

    [Theory]
    [InlineData(TicketPriority.Urgent, 4)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Normal, 24)]
    [InlineData(TicketPriority.Low, 72)]
    public void Response_target_is_measured_from_creation_by_priority(TicketPriority priority, int hours)
    {
        Assert.Equal(Created.AddHours(hours), TicketSla.SlaDue(Ticket(priority)));
    }

    [Fact]
    public void Ticket_becomes_overdue_only_after_its_target_passes()
    {
        var ticket = Ticket(TicketPriority.Urgent);

        Assert.False(TicketSla.IsOverdue(ticket, Created.AddHours(4)));
        Assert.True(TicketSla.IsOverdue(ticket, Created.AddHours(4).AddSeconds(1)));
    }

    [Fact]
    public void Agent_reply_stops_the_clock()
    {
        var ticket = Ticket(TicketPriority.Urgent, firstAgentReply: Created.AddHours(5));

        Assert.False(TicketSla.IsOverdue(ticket, Created.AddDays(2)));
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void Resolved_and_closed_tickets_are_never_overdue_or_answered(TicketStatus status)
    {
        var ticket = Ticket(TicketPriority.Urgent, status, lastReplyBy: TicketActorTypes.Agent);

        Assert.False(TicketSla.IsOverdue(ticket, Created.AddDays(10)));
        Assert.False(TicketSla.IsAnswered(ticket));
    }

    [Theory]
    [InlineData(TicketActorTypes.Agent, true)]
    [InlineData(TicketActorTypes.User, false)]
    [InlineData(null, false)]
    public void Answered_means_the_last_public_reply_came_from_an_agent(string? lastReplyBy, bool expected)
    {
        Assert.Equal(expected, TicketSla.IsAnswered(Ticket(TicketPriority.Normal, TicketStatus.AwaitingUser, lastReplyBy: lastReplyBy)));
    }
}
