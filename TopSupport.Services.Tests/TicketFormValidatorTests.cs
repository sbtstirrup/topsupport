using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Services.Tests.Fakes;
using TopSupport.Services.Tickets;

namespace TopSupport.Services.Tests;

public class TicketFormValidatorTests
{
    private readonly FakeUserRepository _users = new();
    private readonly TicketFormValidator _validator;

    public TicketFormValidatorTests()
    {
        _users.Users.Add(new UserDto { Id = "u1", Name = "Owen Pryce" });
        _users.Users.Add(new UserDto { Id = "u2", Name = "Gone Person", Deleted = true });
        _validator = new TicketFormValidator(_users, new TicketFieldOptionsResolver(_users, new FakeAssetRepository()));
    }

    // "general" has one required field: details.
    private static CreateTicketRequest Valid() => new()
    {
        Topic = "general",
        Subject = "Printer on floor 2 is jammed",
        RequesterUserId = "u1",
        AffectedUserId = "u1",
        Fields = new Dictionary<string, object?> { ["details"] = "It shows a paper jam." }
    };

    private async Task<Dictionary<string, string>> ErrorsFor(CreateTicketRequest request)
        => (await _validator.ValidateAsync(request, agentMode: false, TestContext.Current.CancellationToken)).Errors;

    [Fact]
    public async Task Complete_form_is_valid()
    {
        Assert.Empty(await ErrorsFor(Valid()));
    }

    [Fact]
    public async Task Missing_topic_stops_there()
    {
        var request = Valid();
        request.Topic = "";
        request.Subject = "";

        var errors = await ErrorsFor(request);

        Assert.Equal("Choose a help topic.", errors["topic"]);
        Assert.False(errors.ContainsKey("subject"));
    }

    [Fact]
    public async Task Unknown_topic_is_treated_as_missing()
    {
        var request = Valid();
        request.Topic = "teleporter";

        Assert.Equal("Choose a help topic.", (await ErrorsFor(request))["topic"]);
    }

    [Theory]
    [InlineData("", "Enter a short subject.")]
    [InlineData("  Hi  ", "Make the subject at least 5 characters so the team can find it.")]
    public async Task Subject_must_be_at_least_five_characters(string subject, string message)
    {
        var request = Valid();
        request.Subject = subject;

        Assert.Equal(message, (await ErrorsFor(request))["subject"]);
    }

    [Fact]
    public async Task Subject_over_the_limit_is_rejected()
    {
        var request = Valid();
        request.Subject = new string('a', 301);

        Assert.Equal("Keep the subject to 300 characters or fewer.", (await ErrorsFor(request))["subject"]);
    }

    [Theory]
    [InlineData("nobody")]
    [InlineData("u2")]
    public async Task Requester_and_affected_must_be_existing_users(string userId)
    {
        var request = Valid();
        request.RequesterUserId = userId;
        request.AffectedUserId = userId;

        var errors = await ErrorsFor(request);

        Assert.Equal("We couldn't find that person. Choose someone from the list.", errors["requester"]);
        Assert.Equal("We couldn't find that person. Choose someone from the list.", errors["affected"]);
    }

    [Fact]
    public async Task Required_topic_field_must_be_filled_in()
    {
        var request = Valid();
        request.Fields["details"] = "   ";

        Assert.Equal("Fill in “Details”.", (await ErrorsFor(request))["details"]);
    }

    [Fact]
    public async Task Answer_for_a_field_the_topic_does_not_have_is_rejected()
    {
        var request = Valid();
        request.Fields["isAdmin"] = "yes";

        Assert.Equal("This form has an answer we don't recognise. Reload the page and try again.", (await ErrorsFor(request))["fields"]);
    }

    [Fact]
    public async Task Oversized_answers_are_rejected_before_the_schema_is_checked()
    {
        var request = Valid();
        request.Fields["details"] = new string('x', 16_001);

        Assert.Equal("Your answers are too long. Shorten them and try again.", (await ErrorsFor(request))["fields"]);
    }
}
