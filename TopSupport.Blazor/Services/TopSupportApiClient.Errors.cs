using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TopSupport.Blazor.Services;

/// <summary>
/// A failed Api call whose Message is safe to show the agent: the Api's ProblemDetails "detail"
/// (e.g. "Choose an active agent.") or { message } body when it sent one, otherwise a plain
/// sentence for the status code. Still an HttpRequestException, so existing catch blocks work.
/// </summary>
public class ApiProblemException : HttpRequestException
{
    public ApiProblemException(string message, HttpStatusCode statusCode) : base(message, null, statusCode)
    {
    }

    /// <summary>The message to toast for <paramref name="ex"/>: the Api's own message, or a generic one (never transport details).</summary>
    public static string UserMessage(HttpRequestException ex)
        => ex is ApiProblemException ? ex.Message : "Something went wrong. Check your connection and try again.";
}

public partial class TopSupportApiClient
{
    /// <summary>
    /// Field -> message (first per field) from the Api's 400 ValidationProblemDetails, which every
    /// endpoint uses for invalid input. Key "" holds a message that isn't about one field.
    /// </summary>
    private static async Task<Dictionary<string, string>> ReadValidationErrorsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemBody>(cancellationToken)
            ?? throw new HttpRequestException($"The Api returned {(int)response.StatusCode} with no error body.", null, response.StatusCode);
        return (problem.Errors ?? new()).Where(e => e.Value.Length > 0).ToDictionary(e => e.Key, e => e.Value[0]);
    }

    private sealed record ValidationProblemBody(Dictionary<string, string[]>? Errors, string? Detail);

    /// <summary>EnsureSuccessStatusCode, but throws ApiProblemException carrying the Api's error message.</summary>
    private static async Task EnsureSuccessWithMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        string? message = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var name in new[] { "detail", "message" })
                    {
                        if (doc.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(value.GetString()))
                        {
                            message = value.GetString();
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON -- fall back to the status-code message below.
        }

        message ??= response.StatusCode switch
        {
            HttpStatusCode.NotFound => "We could not find that. It may have been deleted -- refresh and try again.",
            HttpStatusCode.Conflict => "Someone else changed this ticket. Refresh and try again.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.BadRequest => "That change could not be saved. Check the values and try again.",
            _ => "Something went wrong. Try again."
        };

        throw new ApiProblemException(message, response.StatusCode);
    }
}
