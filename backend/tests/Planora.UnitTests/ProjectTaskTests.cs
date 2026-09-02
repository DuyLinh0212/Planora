using Planora.Domain.Tasks;

namespace Planora.UnitTests;

public sealed class ProjectTaskTests
{
    private static readonly Guid ProjectId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset CurrentTime = new(2026, 8, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExpireTaskIfOverdueWithoutSubmissionMarksTaskExpired()
    {
        var projectTask = CreateProjectTask(CurrentTime.AddHours(-1));
        var taskWasExpired = projectTask.ExpireTaskIfOverdue(CurrentTime, hasSubmissionBeforeDeadline: false);
        Assert.True(taskWasExpired);
        Assert.Equal(PlanoraTaskStatus.Expired, projectTask.Status);
        Assert.Equal(CurrentTime, projectTask.ExpiredAt);
    }

    [Fact]
    public void ExpireTaskIfOverdueWithSubmissionKeepsTaskOpen()
    {
        var projectTask = CreateProjectTask(CurrentTime.AddHours(-1));
        var taskWasExpired = projectTask.ExpireTaskIfOverdue(CurrentTime, hasSubmissionBeforeDeadline: true);
        Assert.False(taskWasExpired);
        Assert.Equal(PlanoraTaskStatus.Todo, projectTask.Status);
    }

    [Fact]
    public void ExtendTaskDeadlineReopensExpiredTask()
    {
        var projectTask = CreateProjectTask(CurrentTime.AddHours(-1));
        projectTask.ExpireTaskIfOverdue(CurrentTime, hasSubmissionBeforeDeadline: false);
        var deadlineChange = projectTask.ExtendTaskDeadline(CurrentTime.AddDays(2), true, "Need additional research", UserId, Guid.CreateVersion7(), CurrentTime);
        Assert.True(deadlineChange.CountsAsLate);
        Assert.Equal(DeadlineChangeType.MemberRequestApproved, deadlineChange.ChangeType);
        Assert.Equal(PlanoraTaskStatus.InProgress, projectTask.Status);
    }

    [Fact]
    public void CompleteTaskBeforeSubmissionReturnsFailure()
    {
        var projectTask = CreateProjectTask(CurrentTime.AddDays(1));
        var completionResult = projectTask.CompleteTask(CurrentTime);
        Assert.False(completionResult.IsSuccess);
        Assert.Equal(PlanoraTaskStatus.Todo, projectTask.Status);
    }

    [Fact]
    public void RequestTaskSubmissionReworkWithoutFeedbackReturnsFailure()
    {
        var submission = TaskSubmission.CreateTaskSubmission(Guid.CreateVersion7(), UserId, 1, "Result", CurrentTime);
        var reworkResult = submission.RequestTaskSubmissionRework(Guid.CreateVersion7(), string.Empty, CurrentTime);
        Assert.False(reworkResult.IsSuccess);
        Assert.Equal("submission.feedback_required", reworkResult.Code);
    }

    [Fact]
    public void UpdateCompletedTaskReturnsLockedFailure()
    {
        var projectTask = CreateProjectTask(CurrentTime.AddDays(1));
        projectTask.StartTask(CurrentTime.AddHours(-2));
        projectTask.SubmitTask(CurrentTime.AddHours(-1));
        projectTask.CompleteTask(CurrentTime);

        var result = projectTask.UpdateProjectTask(
            null,
            "Changed title",
            "Changed description",
            ProjectTaskType.Feature,
            TaskPriority.High,
            SubmissionRequirement.Any,
            string.Empty,
            CurrentTime.AddDays(2),
            null,
            false,
            CurrentTime.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("task.locked", result.Code);
    }

    private static ProjectTask CreateProjectTask(DateTimeOffset dueAt) =>
        ProjectTask.CreateProjectTask(ProjectId, null, "Prepare architecture", "Document module boundaries.", TaskPriority.High, dueAt, UserId, CurrentTime.AddDays(-2));
}
