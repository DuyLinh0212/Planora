using Planora.Domain.Common;

namespace Planora.Domain.Tasks;

public sealed class TaskSubmission : Entity
{
    private TaskSubmission() { }
    public Guid TaskId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewFeedback { get; private set; }

    public static TaskSubmission CreateTaskSubmission(Guid taskId, Guid submittedByUserId, int attemptNumber, string? description, DateTimeOffset submittedAt) => new()
    {
        TaskId = taskId,
        SubmittedByUserId = submittedByUserId,
        AttemptNumber = attemptNumber,
        Description = description?.Trim(),
        SubmittedAt = submittedAt,
        Status = SubmissionStatus.PendingReview
    };

    public BusinessRuleResult ApproveTaskSubmission(Guid reviewedByUserId, DateTimeOffset reviewedAt)
    {
        if (Status != SubmissionStatus.PendingReview)
            return BusinessRuleResult.Failure("submission.already_reviewed", "Submission has already been reviewed.");
        Status = SubmissionStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt;
        return BusinessRuleResult.Success();
    }

    public BusinessRuleResult RequestTaskSubmissionRework(Guid reviewedByUserId, string feedback, DateTimeOffset reviewedAt)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            return BusinessRuleResult.Failure("submission.feedback_required", "Rework feedback is required.");
        if (Status != SubmissionStatus.PendingReview)
            return BusinessRuleResult.Failure("submission.already_reviewed", "Submission has already been reviewed.");
        Status = SubmissionStatus.ReworkRequested;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt;
        ReviewFeedback = feedback.Trim();
        return BusinessRuleResult.Success();
    }
}

public sealed class TaskSubmissionLink : Entity
{
    private TaskSubmissionLink() { }
    public Guid SubmissionId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string LinkType { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public static TaskSubmissionLink CreateTaskSubmissionLink(Guid submissionId, string url, string linkType, string? title) => new() { SubmissionId = submissionId, Url = url, LinkType = linkType, Title = title };
}

public sealed class TaskSubmissionFile : Entity
{
    private TaskSubmissionFile() { }
    public Guid SubmissionId { get; private set; }
    public Guid ProjectFileId { get; private set; }
    public Guid FileVersionId { get; private set; }
    public static TaskSubmissionFile AttachFileVersionToTaskSubmission(Guid submissionId, Guid projectFileId, Guid fileVersionId) => new() { SubmissionId = submissionId, ProjectFileId = projectFileId, FileVersionId = fileVersionId };
}
