using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Storage;
using Planora.Domain.Tasks;

namespace Planora.Application.TaskSubmissions;

public sealed class TaskSubmissionService(
    IPlanoraDbContext dbContext, 
    ICurrentUser currentUser, 
    IProjectPermissionService projectPermissionService, 
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<TaskSubmissionDetailResponse>> GetLatestTaskSubmissionAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<TaskSubmissionDetailResponse>(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskViewAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<TaskSubmissionDetailResponse>(authorizationError);

        var submission = await dbContext.TaskSubmissions
            .Where(item => item.TaskId == taskId)
            .OrderByDescending(item => item.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (submission is null)
            return ApplicationResult.Failure<TaskSubmissionDetailResponse>(ApplicationErrors.NotFound("Submission"));
        var submittedBy = await dbContext.Users
            .Where(user => user.Id == submission.SubmittedByUserId)
            .Select(user => user.DisplayName)
            .FirstAsync(cancellationToken);
        var links = await dbContext.TaskSubmissionLinks
            .Where(link => link.SubmissionId == submission.Id)
            .Select(link => new TaskSubmissionLinkResponse(link.Id, link.Url, link.LinkType, link.Title))
            .ToListAsync(cancellationToken);
        var files = await (
            from attachment in dbContext.TaskSubmissionFiles
            join file in dbContext.ProjectFiles on attachment.ProjectFileId equals file.Id
            join version in dbContext.FileVersions on attachment.FileVersionId equals version.Id
            where attachment.SubmissionId == submission.Id
            select new TaskSubmissionFileResponse(file.Id, version.Id, file.Name, file.MimeType, version.SizeBytes))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success(new TaskSubmissionDetailResponse(
            submission.Id,
            submission.TaskId,
            submission.AttemptNumber,
            submission.SubmittedAt,
            submission.Status,
            submission.Description,
            submission.SubmittedByUserId,
            submittedBy,
            links,
            files,
            submission.ReviewFeedback,
            submission.ReviewedAt));
    }

    public async Task<ApplicationResult<TaskSubmissionResponse>> SubmitProjectTaskAsync(Guid taskId, SubmitProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.NotFound("Task"));
        if (currentUser.UserId is not Guid userId || !await projectPermissionService.UserHasPermissionAsync(userId, projectTask.ProjectId, PermissionCodes.TaskSubmit, null, cancellationToken))
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.NotFound("Task"));
        if (!await CurrentUserIsTaskAssigneeAsync(taskId, cancellationToken))
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Forbidden("task.not_assignee", "Only an assignee can submit this task."));
        var links = request.Links ?? [];
        var fileVersionIds = (request.FileVersionIds ?? []).Distinct().ToArray();
        var validLinks = links.Where(link => Uri.TryCreate(link.Url, UriKind.Absolute, out _)).ToArray();
        if (validLinks.Length != links.Count)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.invalid_link", "Every submission link must be an absolute URL.", "links"));
        if (projectTask.SubmissionRequirement == SubmissionRequirement.LinkOnly && validLinks.Length == 0)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.link_required", "This task requires at least one valid submission link.", "links"));
        if (projectTask.SubmissionRequirement == SubmissionRequirement.LinkOnly && fileVersionIds.Length > 0)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.file_not_allowed", "This task accepts links only.", "fileVersionIds"));
        if (projectTask.SubmissionRequirement is not (SubmissionRequirement.Any or SubmissionRequirement.LinkOnly) && fileVersionIds.Length == 0)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.file_required", "This task requires at least one file.", "fileVersionIds"));
        if (projectTask.SubmissionRequirement == SubmissionRequirement.Any && validLinks.Length == 0 && fileVersionIds.Length == 0)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.result_required", "Add at least one link or file before submitting.", "links"));

        var attachedFiles = await (
            from version in dbContext.FileVersions
            join file in dbContext.ProjectFiles on version.ProjectFileId equals file.Id
            where fileVersionIds.Contains(version.Id) && file.ProjectId == projectTask.ProjectId && file.OwnerUserId == userId && file.DeletedAt == null
            select new { Version = version, File = file })
            .ToListAsync(cancellationToken);
        if (attachedFiles.Count != fileVersionIds.Length)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.invalid_file", "Every attached file must be uploaded by you for this project.", "fileVersionIds"));
        foreach (var attachedFile in attachedFiles)
        {
            var extension = Path.GetExtension(attachedFile.File.Name).TrimStart('.').ToLowerInvariant();
            var allowedExtensions = projectTask.AllowedExtensionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if ((allowedExtensions.Length > 0 && !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) ||
                !SubmissionRequirementAcceptsExtension(projectTask.SubmissionRequirement, extension))
                return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Validation("submission.file_type_not_allowed", $"File '{attachedFile.File.Name}' does not match this task's submission requirement.", "fileVersionIds"));
        }

        var currentTime = timeProvider.GetUtcNow();
        var submitResult = projectTask.SubmitTask(currentTime);
        if (!submitResult.IsSuccess)
            return ApplicationResult.Failure<TaskSubmissionResponse>(ApplicationErrors.Conflict(submitResult.Code!, submitResult.Message!));

        var attemptNumber = await dbContext.TaskSubmissions.CountAsync(submission => submission.TaskId == taskId, cancellationToken) + 1;
        var taskSubmission = TaskSubmission.CreateTaskSubmission(taskId, currentUser.UserId!.Value, attemptNumber, request.Description, currentTime);
        dbContext.TaskSubmissions.Add(taskSubmission);
        dbContext.TaskSubmissionLinks.AddRange(validLinks
            .Select(link => TaskSubmissionLink.CreateTaskSubmissionLink(taskSubmission.Id, link.Url, link.LinkType, link.Title)));
        dbContext.TaskSubmissionFiles.AddRange(attachedFiles.Select(item =>
            TaskSubmissionFile.AttachFileVersionToTaskSubmission(taskSubmission.Id, item.File.Id, item.Version.Id)));
        foreach (var attachedFile in attachedFiles)
            attachedFile.File.SetSourceTask(taskId, currentTime);

        var reviewerUserIds = await (
            from member in dbContext.ProjectMembers
            join memberRole in dbContext.ProjectMemberRoles on member.Id equals memberRole.ProjectMemberId
            join rolePermission in dbContext.ProjectRolePermissions on memberRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where member.ProjectId == projectTask.ProjectId && member.Status == MembershipStatus.Active &&
                  permission.Code == PermissionCodes.TaskReview && rolePermission.Effect == PermissionEffect.Allow
            select member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        dbContext.UserNotifications.AddRange(reviewerUserIds.Select(reviewerUserId =>
            Domain.Users.UserNotification.CreateUserNotification(reviewerUserId, "task.submitted", "Có bài nộp mới cần duyệt", projectTask.Title, nameof(TaskSubmission), taskSubmission.Id.ToString(), currentTime)));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectTask.ProjectId, "task.submitted", nameof(ProjectTask), projectTask.Id.ToString(), null, $"{{\"submissionId\":\"{taskSubmission.Id}\",\"attempt\":{attemptNumber}}}", currentUser.IpAddress, currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyProjectAsync(projectTask.ProjectId, "TaskSubmitted", new { taskId = projectTask.Id, title = projectTask.Title, submissionId = taskSubmission.Id }, cancellationToken);

        return ApplicationResult.Success(new TaskSubmissionResponse(taskSubmission.Id, taskId, attemptNumber, taskSubmission.SubmittedAt, taskSubmission.Status));
    }

    public async Task<ApplicationResult> ApproveTaskSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var taskSubmission = await dbContext.TaskSubmissions.FindAsync([submissionId], cancellationToken);
        if (taskSubmission is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Submission"));
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskSubmission.TaskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetTaskReviewAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var currentTime = timeProvider.GetUtcNow();
        var approvalResult = taskSubmission.ApproveTaskSubmission(currentUser.UserId!.Value, currentTime);
        if (!approvalResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(approvalResult.Code!, approvalResult.Message!));
        var completionResult = projectTask.CompleteTask(currentTime);
        if (!completionResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(completionResult.Code!, completionResult.Message!));

        dbContext.UserNotifications.Add(Domain.Users.UserNotification.CreateUserNotification(taskSubmission.SubmittedByUserId, "submission.approved", "Bài nộp đã được duyệt", projectTask.Title, nameof(ProjectTask), projectTask.Id.ToString(), currentTime));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectTask.ProjectId, "submission.approved", nameof(ProjectTask), projectTask.Id.ToString(), null, $"{{\"submissionId\":\"{taskSubmission.Id}\"}}", currentUser.IpAddress, currentTime));

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyUserAsync(taskSubmission.SubmittedByUserId, "NotificationReceived", new { type = "submission.approved", title = "Bài nộp đã được duyệt", message = projectTask.Title, taskId = projectTask.Id }, cancellationToken);

        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RequestTaskSubmissionReworkAsync(Guid submissionId, ReviewTaskSubmissionRequest request, CancellationToken cancellationToken)
    {
        var taskSubmission = await dbContext.TaskSubmissions.FindAsync([submissionId], cancellationToken);
        if (taskSubmission is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Submission"));
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskSubmission.TaskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));

        var authorizationError = await GetTaskReviewAuthorizationErrorAsync(projectTask.ProjectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var currentTime = timeProvider.GetUtcNow();
        var reviewResult = taskSubmission.RequestTaskSubmissionRework(currentUser.UserId!.Value, request.Feedback ?? string.Empty, currentTime);
        if (!reviewResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Validation(reviewResult.Code!, reviewResult.Message!, "feedback"));
        var reworkResult = projectTask.RequestTaskRework(currentTime);
        if (!reworkResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(reworkResult.Code!, reworkResult.Message!));

        dbContext.UserNotifications.Add(Domain.Users.UserNotification.CreateUserNotification(taskSubmission.SubmittedByUserId, "submission.rework", "Bài nộp cần làm lại", request.Feedback ?? string.Empty, nameof(ProjectTask), projectTask.Id.ToString(), currentTime));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(currentUser.UserId, projectTask.ProjectId, "submission.rework_requested", nameof(ProjectTask), projectTask.Id.ToString(), null, $"{{\"submissionId\":\"{taskSubmission.Id}\"}}", currentUser.IpAddress, currentTime));

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyUserAsync(taskSubmission.SubmittedByUserId, "TaskReworkRequested", new { taskId = projectTask.Id, title = projectTask.Title, feedback = request.Feedback }, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(taskSubmission.SubmittedByUserId, "NotificationReceived", new { type = "submission.rework", title = "Bài nộp cần làm lại", message = request.Feedback, taskId = projectTask.Id }, cancellationToken);

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

    private static bool SubmissionRequirementAcceptsExtension(SubmissionRequirement requirement, string extension) => requirement switch
    {
        SubmissionRequirement.Word => extension is "doc" or "docx",
        SubmissionRequirement.Excel => extension is "xls" or "xlsx" or "csv",
        SubmissionRequirement.Pdf => extension == "pdf",
        SubmissionRequirement.PowerPoint => extension is "ppt" or "pptx",
        SubmissionRequirement.Image => extension is "png" or "jpg" or "jpeg" or "gif" or "webp",
        _ => true
    };

    private async Task<ApplicationError?> GetTaskViewAuthorizationErrorAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        var canView = await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, PermissionCodes.TaskView, null, cancellationToken)
            || await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, PermissionCodes.TaskSubmit, null, cancellationToken)
            || await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, PermissionCodes.TaskReview, null, cancellationToken);
        return canView ? null : ApplicationErrors.NotFound("Task");
    }

    private async Task<ApplicationError?> GetTaskReviewAuthorizationErrorAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, PermissionCodes.TaskReview, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Task");
    }
}
