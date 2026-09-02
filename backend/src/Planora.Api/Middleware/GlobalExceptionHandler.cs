using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Planora.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled request failure {TraceId}", httpContext.TraceIdentifier);
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://httpstatuses.com/500",
            Detail = environment.IsDevelopment() ? exception.Message : null
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        await Results.Problem(problemDetails).ExecuteAsync(httpContext);
        return true;
    }
}
