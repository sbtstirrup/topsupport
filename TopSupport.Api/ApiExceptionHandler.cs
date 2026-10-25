using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TopSupport.Api;

/// <summary>
/// Maps exceptions that escape a controller to ProblemDetails: ArgumentException (bad input the
/// service rejected) is a 400 with its message, KeyNotFoundException a 404, anything else a 500
/// with a generic message -- never the exception text, stack or SQL error (OWASP A05).
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
        var (status, title, detail) = exception switch
        {
            // Message without the " (Parameter 'x')" suffix ArgumentException appends.
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Invalid request", ex.ParamName is null ? ex.Message : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty)),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", null),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong", null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
