using System.Net.Http.Json;
using TopSupport.Models.Tickets.FieldSchema;
using TopSupport.Services.Tickets;

namespace TopSupport.Blazor.Components.Shared;

/// <summary>
/// What Raise (end user) and NewTicket (agent) both post as a CreateTicketRequest, and how both
/// read the Api's 400 back. The Api validates everything; the forms only show its errors.
/// </summary>
public static class CreateTicketForm
{
    /// <summary>
    /// Answers to the fields currently shown (TicketFormValidator.VisibleFields, so no Banners or
    /// stale answers behind hidden fields). Files fields are left out: their files are staged and
    /// uploaded once the ticket exists, so the Api never needs the references.
    /// </summary>
    public static Dictionary<string, object?> Answers(IEnumerable<TicketFieldDescriptor> schema, IReadOnlyDictionary<string, object?> values)
        => TicketFormValidator.VisibleFields(schema, values)
            .Where(f => f.Type != TicketFieldType.Files && values.ContainsKey(f.Key))
            .ToDictionary(f => f.Key, f => values[f.Key]);

    /// <summary>
    /// Field key (requester, topic, subject, affected, fields, or a topic field's key) -> first
    /// message from the Api's 400 ValidationProblemDetails. Never empty, so the form's error
    /// summary always says something.
    /// </summary>
    public static async Task<Dictionary<string, string>> ReadErrorsAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemBody>()
            ?? throw new HttpRequestException($"The Api returned {(int)response.StatusCode} with no error body.", null, response.StatusCode);
        var errors = (problem.Errors ?? new()).Where(e => e.Value.Length > 0).ToDictionary(e => e.Key, e => e.Value[0]);
        if (errors.Count == 0) errors["form"] = "We couldn't create the ticket. Check the form and try again.";
        return errors;
    }

    private sealed record ValidationProblemBody(Dictionary<string, string[]>? Errors);
}
