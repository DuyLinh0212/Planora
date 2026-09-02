using Planora.Domain.Tasks;

namespace Planora.Application.TaskDeadlines;

public sealed record RequestTaskDeadlineExtensionRequest(DateTimeOffset RequestedDueAt, string Reason);
public sealed record ExtendTaskDeadlineRequest(DateTimeOffset NewDueAt, string Reason);
public sealed record ReviewTaskDeadlineExtensionRequest(string? Note);
public sealed record TaskDeadlineChangeResponse(
    Guid Id,
    DateTimeOffset OldDueAt,
    DateTimeOffset NewDueAt,
    DeadlineChangeType ChangeType,
    bool CountsAsLate,
    string Reason,
    Guid ChangedByUserId,
    Guid? ExtensionRequestId,
    DateTimeOffset CreatedAt);
public sealed record TaskExtensionRequestResponse(
    Guid Id,
    Guid TaskId,
    Guid RequestedByUserId,
    string RequestedByDisplayName,
    DateTimeOffset RequestedDueAt,
    string Reason,
    ExtensionRequestStatus Status,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote,
    DateTimeOffset CreatedAt);
