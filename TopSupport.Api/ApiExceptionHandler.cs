using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Services;
using TopSupport.Services.Tickets;

namespace TopSupport.Api;

/// <summary>
/// Maps exceptions that escape a controller to ProblemDetails:
/// - ValidationException (the user's input broke a rule) -> 400 ValidationProblemDetails, the same
///   shape as model-binding errors: field -> messages, plus every message joined in Detail
/// - TicketConflictException -> 409, KeyNotFoundException -> 404
/// - anything else, ArgumentException included (a bug, not bad input) -> 500 with a generic
///   message, logged; never the exception text, stack or SQL error (OWASP A05).
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ex => new ValidationProblemDetails(ex.Errors.ToDictionary(e => e.Key, e => new[] { e.Value }))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = string.Join(" ", ex.Errors.Values)
            },
            TicketConflictException ex => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message },
            KeyNotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not found" },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Something went wrong" }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else if (exception is KeyNotFoundException)
        {
            // The message names what wasn't found (e.g. "Ticket 'T060099' was not found."); it isn't sent to the caller.
            _logger.LogInformation("404 for {Method} {Path}: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
