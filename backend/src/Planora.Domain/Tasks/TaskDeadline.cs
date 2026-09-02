using Planora.Domain.Common;

namespace Planora.Domain.Tasks;

public sealed class TaskExtensionRequest : Entity
{
    private TaskExtensionRequest() { }
    public Guid TaskId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public DateTimeOffset RequestedDueAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ExtensionRequestStatus Status { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static TaskExtensionRequest CreateTaskExtensionRequest(Guid taskId, Guid requestedByUserId, DateTimeOffset requestedDueAt, string reason, DateTimeOffset createdAt) => new()
    {
        TaskId = taskId,
        RequestedByUserId = requestedByUserId,
        RequestedDueAt = requestedDueAt,
        Reason = reason.Trim(),
        Status = ExtensionRequestStatus.Pending,
        CreatedAt = createdAt
    };

    public BusinessRuleResult ApproveTaskExtension(Guid reviewedByUserId, string? reviewNote, DateTimeOffset reviewedAt)
    {
        if (Status != ExtensionRequestStatus.Pending)
            return BusinessRuleResult.Failure("extension.not_pending", "Extension request is no longer pending.");
        Status = ExtensionRequestStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt;
        ReviewNote = reviewNote;
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult RejectTaskExtension(Guid reviewedByUserId, string? reviewNote, DateTimeOffset reviewedAt)
    {
        if (Status != ExtensionRequestStatus.Pending)
            return BusinessRuleResult.Failure("extension.not_pending", "Extension request is no longer pending.");
        Status = ExtensionRequestStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt;
        ReviewNote = reviewNote;
        return BusinessRuleResult.Success();
    }
}

public sealed class TaskDeadlineChange : Entity
{
    private TaskDeadlineChange() { }
    public Guid TaskId { get; private set; }
    public DateTimeOffset OldDueAt { get; private set; }
    public DateTimeOffset NewDueAt { get; private set; }
    public DeadlineChangeType ChangeType { get; private set; }
    public bool CountsAsLate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid ChangedByUserId { get; private set; }
    public Guid? ExtensionRequestId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public static TaskDeadlineChange CreateTaskDeadlineChange(Guid taskId, DateTimeOffset oldDueAt, DateTimeOffset newDueAt, DeadlineChangeType changeType, bool countsAsLate, string reason, Guid changedByUserId, Guid? extensionRequestId, DateTimeOffset createdAt) => new()
    {
        TaskId = taskId,
        OldDueAt = oldDueAt,
        NewDueAt = newDueAt,
        ChangeType = changeType,
        CountsAsLate = countsAsLate,
        Reason = reason.Trim(),
        ChangedByUserId = changedByUserId,
        ExtensionRequestId = extensionRequestId,
        CreatedAt = createdAt
    };
}
