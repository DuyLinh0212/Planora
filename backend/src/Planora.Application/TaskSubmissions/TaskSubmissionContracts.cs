using Planora.Domain.Tasks;

namespace Planora.Application.TaskSubmissions;

public sealed record SubmitProjectTaskRequest(
    string? Description,
    IReadOnlyList<TaskSubmissionLinkRequest> Links,
    IReadOnlyList<Guid>? FileVersionIds = null);
public sealed record TaskSubmissionLinkRequest(string Url, string LinkType, string? Title);
public sealed record ReviewTaskSubmissionRequest(string? Feedback);
public sealed record TaskSubmissionResponse(Guid Id, Guid TaskId, int AttemptNumber, DateTimeOffset SubmittedAt, SubmissionStatus Status);
public sealed record TaskSubmissionLinkResponse(Guid Id, string Url, string LinkType, string? Title);
public sealed record TaskSubmissionFileResponse(Guid ProjectFileId, Guid FileVersionId, string Name, string MimeType, long SizeBytes);
public sealed record TaskSubmissionDetailResponse(
    Guid Id,
    Guid TaskId,
    int AttemptNumber,
    DateTimeOffset SubmittedAt,
    SubmissionStatus Status,
    string? Description,
    Guid SubmittedByUserId,
    string SubmittedByDisplayName,
    IReadOnlyList<TaskSubmissionLinkResponse> Links,
    IReadOnlyList<TaskSubmissionFileResponse> Files,
    string? ReviewFeedback = null,
    DateTimeOffset? ReviewedAt = null);
