namespace Planora.Application.Common.Results;

public enum ApplicationErrorType { Validation, NotFound, Conflict, Forbidden, Unauthorized, External }

public sealed record ApplicationError(string Code, string Message, ApplicationErrorType Type, string? Field = null);

public class ApplicationResult
{
    protected ApplicationResult(bool isSuccess, IReadOnlyList<ApplicationError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<ApplicationError> Errors { get; }

    public static ApplicationResult Success() => new(true, []);
    public static ApplicationResult Failure(params ApplicationError[] errors) => new(false, errors);
    public static ApplicationResult<T> Success<T>(T value) => new(value, []);
    public static ApplicationResult<T> Failure<T>(params ApplicationError[] errors) => new(default, errors);
}

public sealed class ApplicationResult<T> : ApplicationResult
{
    internal ApplicationResult(T? value, IReadOnlyList<ApplicationError> errors) : base(errors.Count == 0, errors) => Value = value;
    public T? Value { get; }
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class ApplicationErrors
{
    public static ApplicationError Validation(string code, string message, string? field = null) => new(code, message, ApplicationErrorType.Validation, field);
    public static ApplicationError NotFound(string resource) => new("not_found", $"{resource} was not found.", ApplicationErrorType.NotFound);
    public static ApplicationError Conflict(string code, string message) => new(code, message, ApplicationErrorType.Conflict);
    public static ApplicationError Forbidden(string code = "forbidden", string message = "You do not have permission to perform this action.") => new(code, message, ApplicationErrorType.Forbidden);
    public static ApplicationError Unauthorized(string message = "Authentication is required.") => new("unauthorized", message, ApplicationErrorType.Unauthorized);
    public static ApplicationError External(string code, string message) => new(code, message, ApplicationErrorType.External);
}
