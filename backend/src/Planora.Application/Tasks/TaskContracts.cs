using Planora.Domain.Tasks;

namespace Planora.Application.Tasks;

public sealed record CreateProjectTaskRequest(
    Guid? SprintId,
    string Title,
    string Description,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    IReadOnlyList<string> AcceptanceCriteria,
    string Type = "General",
    SubmissionRequirement SubmissionRequirement = SubmissionRequirement.Any,
    IReadOnlyList<string>? AllowedExtensions = null,
    Guid? DependsOnTaskId = null,
    bool IsMilestone = false);
public sealed record UpdateProjectTaskRequest(
    Guid? SprintId,
    string Title,
    string Description,
    TaskPriority Priority,
    DateTimeOffset? DueAt,
    IReadOnlyList<string> AcceptanceCriteria,
    string Type,
    SubmissionRequirement SubmissionRequirement,
    IReadOnlyList<string>? AllowedExtensions,
    Guid? DependsOnTaskId,
    bool IsMilestone);
public sealed record AssignProjectMemberRequest(Guid ProjectMemberId);
public sealed record ProjectTaskResponse(
    Guid Id,
    Guid ProjectId,
    Guid? SprintId,
    string Title,
    string Description,
    TaskPriority Priority,
    PlanoraTaskStatus Status,
    DateTimeOffset? OriginalDueAt,
    DateTimeOffset? EffectiveDueAt,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<Guid> AssigneeMemberIds,
    string Type,
    SubmissionRequirement SubmissionRequirement,
    IReadOnlyList<string> AllowedExtensions,
    Guid? DependsOnTaskId,
    bool IsMilestone);
public sealed record ProjectTaskHistoryResponse(Guid Id, Guid? ActorUserId, string ActorDisplayName, string Action, string? BeforeJson, string? AfterJson, DateTimeOffset CreatedAt);
