using Planora.Domain.Common;

namespace Planora.Domain.Tasks;

public sealed class ProjectTask : AuditableEntity
{
    private ProjectTask() { }

    public Guid ProjectId { get; private set; }
    public Guid? SprintId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Type { get; private set; } = nameof(ProjectTaskType.General);
    public SubmissionRequirement SubmissionRequirement { get; private set; }
    public string AllowedExtensionsCsv { get; private set; } = string.Empty;
    public Guid? DependsOnTaskId { get; private set; }
    public bool IsMilestone { get; private set; }
    public TaskPriority Priority { get; private set; }
    public PlanoraTaskStatus Status { get; private set; }
    public DateTimeOffset? OriginalDueAt { get; private set; }
    public DateTimeOffset? EffectiveDueAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static ProjectTask CreateProjectTask(Guid projectId, Guid? sprintId, string title, string description, TaskPriority priority, DateTimeOffset? dueAt, Guid createdByUserId, DateTimeOffset createdAt)
        => CreateProjectTask(projectId, sprintId, title, description, ProjectTaskType.General, priority, SubmissionRequirement.Any, string.Empty, dueAt, null, false, createdByUserId, createdAt);

    public static ProjectTask CreateProjectTask(
        Guid projectId,
        Guid? sprintId,
        string title,
        string description,
        string type,
        TaskPriority priority,
        SubmissionRequirement submissionRequirement,
        string allowedExtensionsCsv,
        DateTimeOffset? dueAt,
        Guid? dependsOnTaskId,
        bool isMilestone,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        var task = new ProjectTask
        {
            ProjectId = projectId,
            SprintId = sprintId,
            Title = title.Trim(),
            Description = description.Trim(),
            Type = type.Trim(),
            SubmissionRequirement = submissionRequirement,
            AllowedExtensionsCsv = allowedExtensionsCsv,
            DependsOnTaskId = dependsOnTaskId,
            IsMilestone = isMilestone,
            Priority = priority,
            Status = PlanoraTaskStatus.Todo,
            OriginalDueAt = dueAt,
            EffectiveDueAt = dueAt,
            CreatedByUserId = createdByUserId
        };
        task.MarkCreated(createdAt);
        return task;
    }

    public BusinessRuleResult UpdateProjectTask(
        Guid? sprintId,
        string title,
        string description,
        string type,
        TaskPriority priority,
        SubmissionRequirement submissionRequirement,
        string allowedExtensionsCsv,
        DateTimeOffset? dueAt,
        Guid? dependsOnTaskId,
        bool isMilestone,
        DateTimeOffset updatedAt)
    {
        if (Status is PlanoraTaskStatus.Done or PlanoraTaskStatus.Cancelled)
            return BusinessRuleResult.Failure("task.locked", "Completed or cancelled tasks cannot be edited.");
        SprintId = sprintId;
        Title = title.Trim();
        Description = description.Trim();
        Type = type.Trim();
        Priority = priority;
        SubmissionRequirement = submissionRequirement;
        AllowedExtensionsCsv = allowedExtensionsCsv;
        DependsOnTaskId = dependsOnTaskId;
        IsMilestone = isMilestone;
        OriginalDueAt = dueAt;
        EffectiveDueAt = dueAt;
        MarkUpdated(updatedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult UpdateProjectTask(
        Guid? sprintId,
        string title,
        string description,
        ProjectTaskType type,
        TaskPriority priority,
        SubmissionRequirement submissionRequirement,
        string allowedExtensionsCsv,
        DateTimeOffset? dueAt,
        Guid? dependsOnTaskId,
        bool isMilestone,
        DateTimeOffset updatedAt) => UpdateProjectTask(
            sprintId, title, description, type.ToString(), priority, submissionRequirement,
            allowedExtensionsCsv, dueAt, dependsOnTaskId, isMilestone, updatedAt);

    public static ProjectTask CreateProjectTask(
        Guid projectId,
        Guid? sprintId,
        string title,
        string description,
        ProjectTaskType type,
        TaskPriority priority,
        SubmissionRequirement submissionRequirement,
        string allowedExtensionsCsv,
        DateTimeOffset? dueAt,
        Guid? dependsOnTaskId,
        bool isMilestone,
        Guid createdByUserId,
        DateTimeOffset createdAt) => CreateProjectTask(
            projectId, sprintId, title, description, type.ToString(), priority,
            submissionRequirement, allowedExtensionsCsv, dueAt, dependsOnTaskId,
            isMilestone, createdByUserId, createdAt);

    public void DeleteProjectTask(DateTimeOffset deletedAt)
    {
        DeletedAt = deletedAt;
        Status = PlanoraTaskStatus.Cancelled;
        MarkUpdated(deletedAt);
    }

    public BusinessRuleResult StartTask(DateTimeOffset startedAt)
    {
        if (Status is not (PlanoraTaskStatus.Todo or PlanoraTaskStatus.Rework))
            return BusinessRuleResult.Failure("task.cannot_start", "Only a TODO or REWORK task can start.");
        Status = PlanoraTaskStatus.InProgress;
        MarkUpdated(startedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult SubmitTask(DateTimeOffset submittedAt)
    {
        if (Status is not (PlanoraTaskStatus.Todo or PlanoraTaskStatus.InProgress or PlanoraTaskStatus.Rework))
            return BusinessRuleResult.Failure("task.cannot_submit", "Task is not in a submittable state.");
        Status = PlanoraTaskStatus.Submitted;
        MarkUpdated(submittedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult CompleteTask(DateTimeOffset completedAt)
    {
        if (Status != PlanoraTaskStatus.Submitted)
            return BusinessRuleResult.Failure("task.not_submitted", "Task can be completed only after an approved submission.");
        Status = PlanoraTaskStatus.Done;
        CompletedAt = completedAt;
        MarkUpdated(completedAt);
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult RequestTaskRework(DateTimeOffset requestedAt)
    {
        if (Status != PlanoraTaskStatus.Submitted)
            return BusinessRuleResult.Failure("task.not_submitted", "Only a submitted task can require rework.");
        Status = PlanoraTaskStatus.Rework;
        MarkUpdated(requestedAt);
        return BusinessRuleResult.Success();
    }

    public bool ExpireTaskIfOverdue(DateTimeOffset currentTime, bool hasSubmissionBeforeDeadline)
    {
        if (EffectiveDueAt is null || EffectiveDueAt >= currentTime || hasSubmissionBeforeDeadline)
            return false;
        if (Status is not (PlanoraTaskStatus.Todo or PlanoraTaskStatus.InProgress or PlanoraTaskStatus.Rework))
            return false;
        Status = PlanoraTaskStatus.Expired;
        ExpiredAt = currentTime;
        MarkUpdated(currentTime);
        return true;
    }

    public TaskDeadlineChange ExtendTaskDeadline(DateTimeOffset newDueAt, bool countsAsLate, string reason, Guid changedByUserId, Guid? extensionRequestId, DateTimeOffset changedAt)
    {
        if (EffectiveDueAt is null)
            throw new InvalidOperationException("A task without a deadline cannot be extended.");
        if (newDueAt <= EffectiveDueAt)
            throw new ArgumentException("New deadline must be later than the effective deadline.", nameof(newDueAt));

        var deadlineChange = TaskDeadlineChange.CreateTaskDeadlineChange(Id, EffectiveDueAt.Value, newDueAt, countsAsLate ? DeadlineChangeType.MemberRequestApproved : DeadlineChangeType.LeaderDirect, countsAsLate, reason, changedByUserId, extensionRequestId, changedAt);
        EffectiveDueAt = newDueAt;
        if (Status == PlanoraTaskStatus.Expired)
        {
            Status = PlanoraTaskStatus.InProgress;
            ExpiredAt = null;
        }
        MarkUpdated(changedAt);
        return deadlineChange;
    }
}
