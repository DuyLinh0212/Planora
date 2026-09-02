using Microsoft.AspNetCore.Mvc;
using Planora.Application.Common.Results;

namespace Planora.Api.Extensions;

public static class ApplicationResultExtensions
{
    public static IResult ToHttpResult(this ApplicationResult result, int successStatusCode = StatusCodes.Status204NoContent) =>
        result.IsSuccess ? Results.StatusCode(successStatusCode) : CreateProblemResult(result.Errors);

    public static IResult ToHttpResult<T>(this ApplicationResult<T> result, int successStatusCode = StatusCodes.Status200OK) =>
        result.IsSuccess ? Results.Json(result.Value, statusCode: successStatusCode) : CreateProblemResult(result.Errors);

    private static IResult CreateProblemResult(IReadOnlyList<ApplicationError> errors)
    {
        var primaryError = errors.FirstOrDefault() ?? ApplicationErrors.External("unknown", "An unexpected error occurred.");
        var statusCode = primaryError.Type switch
        {
            ApplicationErrorType.Validation => StatusCodes.Status400BadRequest,
            ApplicationErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ApplicationErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ApplicationErrorType.NotFound => StatusCodes.Status404NotFound,
            ApplicationErrorType.Conflict => StatusCodes.Status409Conflict,
            ApplicationErrorType.External => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };
        var problemDetails = new ProblemDetails { Status = statusCode, Title = primaryError.Message, Type = $"https://httpstatuses.com/{statusCode}" };
        problemDetails.Extensions["code"] = primaryError.Code;
        problemDetails.Extensions["errors"] = errors.Select(error => new { error.Code, error.Message, error.Field }).ToArray();
        return Results.Problem(problemDetails);
    }
}
