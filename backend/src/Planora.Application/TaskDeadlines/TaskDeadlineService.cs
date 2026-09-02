using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Tasks;

namespace Planora.Application.TaskDeadlines;

public sealed class TaskDeadlineService(IPlanoraDbContext dbContext, ICurrentUser currentUser, IProjectPermissionService projectPermissionService, TimeProvider timeProvider)
{
    public async Task<ApplicationResult<IReadOnlyList<TaskDeadlineChangeResponse>>> GetTaskDeadlineHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<IReadOnlyList<TaskDeadlineChangeResponse>>(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetDeadlineAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<TaskDeadlineChangeResponse>>(authorizationError);

        var history = await dbContext.TaskDeadlineChanges
            .Where(change => change.TaskId == taskId)
            .OrderByDescending(change => change.CreatedAt)
            .Select(change => new TaskDeadlineChangeResponse(
                change.Id,
                change.OldDueAt,
                change.NewDueAt,
                change.ChangeType,
                change.CountsAsLate,
                change.Reason,
                change.ChangedByUserId,
                change.ExtensionRequestId,
                change.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success<IReadOnlyList<TaskDeadlineChangeResponse>>(history);
    }

    public async Task<ApplicationResult<IReadOnlyList<TaskExtensionRequestResponse>>> GetTaskExtensionRequestsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<IReadOnlyList<TaskExtensionRequestResponse>>(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetDeadlineAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null && !await CurrentUserIsTaskAssigneeAsync(taskId, cancellationToken))
            return ApplicationResult.Failure<IReadOnlyList<TaskExtensionRequestResponse>>(authorizationError);

        var requests = await (from req in dbContext.TaskExtensionRequests
                              join user in dbContext.Users on req.RequestedByUserId equals user.Id
                              where req.TaskId == taskId
                              orderby req.CreatedAt descending
                              select new TaskExtensionRequestResponse(
                                  req.Id,
                                  req.TaskId,
                                  req.RequestedByUserId,
                                  user.DisplayName,
                                  req.RequestedDueAt,
                                  req.Reason,
                                  req.Status,
                                  req.ReviewedByUserId,
                                  req.ReviewedAt,
                                  req.ReviewNote,
                                  req.CreatedAt)).ToListAsync(cancellationToken);

        return ApplicationResult.Success<IReadOnlyList<TaskExtensionRequestResponse>>(requests);
    }

    public async Task<ApplicationResult<Guid>> RequestTaskDeadlineExtensionAsync(Guid taskId, RequestTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<Guid>(ApplicationErrors.NotFound("Task"));
        if (!await CurrentUserIsTaskAssigneeAsync(taskId, cancellationToken))
            return ApplicationResult.Failure<Guid>(ApplicationErrors.Forbidden("task.not_assignee", "Only an assignee can request an extension."));
        if (projectTask.Status is PlanoraTaskStatus.Done or PlanoraTaskStatus.Cancelled)
            return ApplicationResult.Failure<Guid>(ApplicationErrors.Conflict("extension.task_closed", "Completed or cancelled tasks cannot be extended."));
        if (projectTask.EffectiveDueAt is null || request.RequestedDueAt <= projectTask.EffectiveDueAt)
            return ApplicationResult.Failure<Guid>(ApplicationErrors.Validation("extension.invalid_due", "Requested deadline must be later than the current deadline."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApplicationResult.Failure<Guid>(ApplicationErrors.Validation("extension.reason_required", "Extension reason is required.", "reason"));

        var sprintBoundaryError = await GetSprintBoundaryErrorAsync(projectTask, request.RequestedDueAt, cancellationToken);
        if (sprintBoundaryError is not null)
            return ApplicationResult.Failure<Guid>(sprintBoundaryError);

        var alreadyPending = await dbContext.TaskExtensionRequests.AnyAsync(
            extension => extension.TaskId == taskId && extension.Status == ExtensionRequestStatus.Pending,
            cancellationToken);
        if (alreadyPending)
            return ApplicationResult.Failure<Guid>(ApplicationErrors.Conflict("extension.pending_exists", "This task already has a pending extension request."));

        var extensionRequest = TaskExtensionRequest.CreateTaskExtensionRequest(taskId, currentUser.UserId!.Value, request.RequestedDueAt, request.Reason, timeProvider.GetUtcNow());
        dbContext.TaskExtensionRequests.Add(extensionRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(extensionRequest.Id);
    }

    public async Task<ApplicationResult> ApproveTaskDeadlineExtensionAsync(Guid extensionRequestId, ReviewTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var extensionRequest = await dbContext.TaskExtensionRequests.FindAsync([extensionRequestId], cancellationToken);
        if (extensionRequest is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Extension request"));
        var projectTask = await dbContext.ProjectTasks.FindAsync([extensionRequest.TaskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetDeadlineAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (projectTask.Status is PlanoraTaskStatus.Done or PlanoraTaskStatus.Cancelled)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("extension.task_closed", "Completed or cancelled tasks cannot be extended."));

        var sprintBoundaryError = await GetSprintBoundaryErrorAsync(projectTask, extensionRequest.RequestedDueAt, cancellationToken);
        if (sprintBoundaryError is not null)
            return ApplicationResult.Failure(sprintBoundaryError);

        var currentTime = timeProvider.GetUtcNow();
        var approvalResult = extensionRequest.ApproveTaskExtension(currentUser.UserId!.Value, request.Note, currentTime);
        if (!approvalResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(approvalResult.Code!, approvalResult.Message!));

        try
        {
            dbContext.TaskDeadlineChanges.Add(projectTask.ExtendTaskDeadline(extensionRequest.RequestedDueAt, true, extensionRequest.Reason, currentUser.UserId.Value, extensionRequest.Id, currentTime));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return ApplicationResult.Failure(ApplicationErrors.Conflict("extension.invalid", exception.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RejectTaskDeadlineExtensionAsync(Guid extensionRequestId, ReviewTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var extensionRequest = await dbContext.TaskExtensionRequests.FindAsync([extensionRequestId], cancellationToken);
        if (extensionRequest is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Extension request"));
        var projectTask = await dbContext.ProjectTasks.FindAsync([extensionRequest.TaskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetDeadlineAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var rejectionResult = extensionRequest.RejectTaskExtension(currentUser.UserId!.Value, request.Note, timeProvider.GetUtcNow());
        if (!rejectionResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(rejectionResult.Code!, rejectionResult.Message!));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> ExtendTaskDeadlineAsync(Guid taskId, ExtendTaskDeadlineRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetDeadlineAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (projectTask.Status is PlanoraTaskStatus.Done or PlanoraTaskStatus.Cancelled)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("extension.task_closed", "Completed or cancelled tasks cannot be extended."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApplicationResult.Failure(ApplicationErrors.Validation("extension.reason_required", "Extension reason is required.", "reason"));

        var sprintBoundaryError = await GetSprintBoundaryErrorAsync(projectTask, request.NewDueAt, cancellationToken);
        if (sprintBoundaryError is not null)
            return ApplicationResult.Failure(sprintBoundaryError);

        try
        {
            dbContext.TaskDeadlineChanges.Add(projectTask.ExtendTaskDeadline(request.NewDueAt, false, request.Reason, currentUser.UserId!.Value, null, timeProvider.GetUtcNow()));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return ApplicationResult.Failure(ApplicationErrors.Conflict("extension.invalid", exception.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<bool> CurrentUserIsTaskAssigneeAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return false;
        return await (
            from taskAssignee in dbContext.TaskAssignees
            join projectMember in dbContext.ProjectMembers on taskAssignee.ProjectMemberId equals projectMember.Id
            where taskAssignee.TaskId == taskId && projectMember.UserId == currentUser.UserId && projectMember.Status == MembershipStatus.Active
            select taskAssignee).AnyAsync(cancellationToken);
    }

    private async Task<ApplicationError?> GetDeadlineAuthorizationErrorAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, PermissionCodes.TaskExtendDeadline, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Task");
    }

    private async Task<ApplicationError?> GetSprintBoundaryErrorAsync(
        ProjectTask projectTask,
        DateTimeOffset requestedDueAt,
        CancellationToken cancellationToken)
    {
        if (projectTask.SprintId is not Guid sprintId)
            return null;

        var sprintEndAt = await dbContext.Sprints
            .Where(sprint => sprint.Id == sprintId && sprint.ProjectId == projectTask.ProjectId)
            .Select(sprint => (DateTimeOffset?)sprint.EndAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sprintEndAt is null)
            return ApplicationErrors.NotFound("Sprint");

        return requestedDueAt <= sprintEndAt.Value
            ? null
            : ApplicationErrors.Validation(
                "extension.outside_sprint",
                "Requested deadline must stay within the task sprint.",
                "requestedDueAt");
    }
}
