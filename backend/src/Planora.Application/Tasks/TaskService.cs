using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Application.Notifications;
using Planora.Domain.Projects;
using Planora.Domain.Tasks;
using Planora.Domain.Users;

namespace Planora.Application.Tasks;

public sealed class TaskService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService,
    TaskEmailNotificationService taskEmailNotificationService,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<ProjectTaskResponse>> CreateProjectTaskAsync(Guid projectId, CreateProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectId, PermissionCodes.TaskCreate, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectTaskResponse>(authorizationError);
        var validationError = await ValidateTaskRequestAsync(projectId, request.SprintId, request.Title, request.DueAt, request.DependsOnTaskId, cancellationToken);
        if (validationError is not null)
            return ApplicationResult.Failure<ProjectTaskResponse>(validationError);
        var taskType = NormalizeTaskType(request.Type);
        if (taskType is null)
            return ApplicationResult.Failure<ProjectTaskResponse>(ApplicationErrors.Validation("task.invalid_type", "Task type must contain 1-40 readable characters.", "type"));

        var now = timeProvider.GetUtcNow();
        var allowedExtensions = NormalizeExtensions(request.AllowedExtensions);
        var projectTask = ProjectTask.CreateProjectTask(
            projectId,
            request.SprintId,
            request.Title,
            request.Description,
            taskType,
            request.Priority,
            request.SubmissionRequirement,
            string.Join(',', allowedExtensions),
            request.DueAt,
            request.DependsOnTaskId,
            request.IsMilestone,
            currentUser.UserId!.Value,
            now);
        dbContext.ProjectTasks.Add(projectTask);
        var acceptanceCriteria = CreateAcceptanceCriteria(projectTask.Id, request.AcceptanceCriteria);
        dbContext.TaskAcceptanceCriteria.AddRange(acceptanceCriteria);
        dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.created", null, SerializeTask(projectTask), now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(MapProjectTaskResponse(projectTask, acceptanceCriteria.Select(item => item.Content).ToArray(), []));
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectTaskResponse>>> GetProjectTasksAsync(Guid projectId, Guid? sprintId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectId, PermissionCodes.TaskView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectTaskResponse>>(authorizationError);

        var taskQuery = dbContext.ProjectTasks.Where(task => task.ProjectId == projectId && task.DeletedAt == null);
        if (sprintId is not null)
            taskQuery = taskQuery.Where(task => task.SprintId == sprintId);

        var projectTasks = await taskQuery.OrderBy(task => task.Status).ThenBy(task => task.EffectiveDueAt).ToListAsync(cancellationToken);
        var taskIds = projectTasks.Select(task => task.Id).ToArray();
        var acceptanceCriteria = await dbContext.TaskAcceptanceCriteria.Where(item => taskIds.Contains(item.TaskId)).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var taskAssignees = await dbContext.TaskAssignees.Where(item => taskIds.Contains(item.TaskId)).ToListAsync(cancellationToken);
        var response = projectTasks.Select(task => MapProjectTaskResponse(
            task,
            acceptanceCriteria.Where(item => item.TaskId == task.Id).Select(item => item.Content).ToArray(),
            taskAssignees.Where(item => item.TaskId == task.Id).Select(item => item.ProjectMemberId).ToArray())).ToArray();
        return ApplicationResult.Success<IReadOnlyList<ProjectTaskResponse>>(response);
    }

    public async Task<ApplicationResult<ProjectTaskResponse>> GetProjectTaskByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<ProjectTaskResponse>(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<ProjectTaskResponse>(authorizationError);
        var criteria = await dbContext.TaskAcceptanceCriteria.Where(item => item.TaskId == taskId).OrderBy(item => item.SortOrder).Select(item => item.Content).ToListAsync(cancellationToken);
        var assignees = await dbContext.TaskAssignees.Where(item => item.TaskId == taskId).Select(item => item.ProjectMemberId).ToListAsync(cancellationToken);
        return ApplicationResult.Success(MapProjectTaskResponse(projectTask, criteria, assignees));
    }

    public async Task<ApplicationResult> UpdateProjectTaskAsync(Guid taskId, UpdateProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskEdit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var validationError = await ValidateTaskRequestAsync(projectTask.ProjectId, request.SprintId, request.Title, request.DueAt, request.DependsOnTaskId, cancellationToken, taskId);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);
        var taskType = NormalizeTaskType(request.Type);
        if (taskType is null)
            return ApplicationResult.Failure(ApplicationErrors.Validation("task.invalid_type", "Task type must contain 1-40 readable characters.", "type"));

        var beforeJson = SerializeTask(projectTask);
        var now = timeProvider.GetUtcNow();
        var updateResult = projectTask.UpdateProjectTask(
            request.SprintId,
            request.Title,
            request.Description,
            taskType,
            request.Priority,
            request.SubmissionRequirement,
            string.Join(',', NormalizeExtensions(request.AllowedExtensions)),
            request.DueAt,
            request.DependsOnTaskId,
            request.IsMilestone,
            now);
        if (!updateResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(updateResult.Code!, updateResult.Message!));

        var currentCriteria = await dbContext.TaskAcceptanceCriteria.Where(item => item.TaskId == taskId).ToListAsync(cancellationToken);
        dbContext.TaskAcceptanceCriteria.RemoveRange(currentCriteria);
        dbContext.TaskAcceptanceCriteria.AddRange(CreateAcceptanceCriteria(taskId, request.AcceptanceCriteria));
        var assigneeUserIds = await (
            from assignee in dbContext.TaskAssignees
            join member in dbContext.ProjectMembers on assignee.ProjectMemberId equals member.Id
            where assignee.TaskId == taskId && member.Status == MembershipStatus.Active
            select member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        dbContext.UserNotifications.AddRange(assigneeUserIds.Select(userId =>
            UserNotification.CreateUserNotification(
                userId,
                "task.updated",
                "Công việc đã được cập nhật",
                projectTask.Title,
                nameof(ProjectTask),
                projectTask.Id.ToString(),
                now)));
        dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.updated", beforeJson, SerializeTask(projectTask), now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await taskEmailNotificationService.QueueTaskEventEmailsAsync(
            projectTask,
            currentUser.UserId!.Value,
            assigneeUserIds,
            TaskEmailEvent.Updated,
            cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DeleteProjectTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FirstOrDefaultAsync(task => task.Id == taskId && task.DeletedAt == null, cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskEdit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var beforeJson = SerializeTask(projectTask);
        var now = timeProvider.GetUtcNow();
        // Tasks and their dependent submission records are disposable project
        // content. Unlike projects and sprints, they must not enter the recovery
        // bin: the database cascades remove their child rows in the same unit.
        dbContext.ProjectTasks.Remove(projectTask);
        dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.deleted", beforeJson, null, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectTaskHistoryResponse>>> GetProjectTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.IgnoreQueryFilters().FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectTaskHistoryResponse>>(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectTaskHistoryResponse>>(authorizationError);
        var history = await (from log in dbContext.AuditLogs
                             join user in dbContext.Users on log.ActorUserId equals user.Id into actors
                             from actor in actors.DefaultIfEmpty()
                             where log.EntityType == nameof(ProjectTask) && log.EntityId == taskId.ToString()
                             orderby log.CreatedAt descending
                             select new ProjectTaskHistoryResponse(log.Id, log.ActorUserId, actor == null ? "System" : actor.DisplayName, log.Action, log.BeforeJson, log.AfterJson, log.CreatedAt))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<ProjectTaskHistoryResponse>>(history);
    }

    public async Task<ApplicationResult> StartProjectTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskSubmit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (!await CurrentUserIsTaskAssigneeAsync(taskId, cancellationToken))
            return ApplicationResult.Failure(ApplicationErrors.Forbidden("task.not_assignee", "Only an assignee can start this task."));
        if (projectTask.DependsOnTaskId is Guid dependencyId && !await dbContext.ProjectTasks.AnyAsync(task => task.Id == dependencyId && task.Status == PlanoraTaskStatus.Done, cancellationToken))
            return ApplicationResult.Failure(ApplicationErrors.Conflict("task.dependency_incomplete", "Complete the dependency before starting this task."));

        var beforeJson = SerializeTask(projectTask);
        var now = timeProvider.GetUtcNow();
        var startResult = projectTask.StartTask(now);
        if (!startResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(startResult.Code!, startResult.Message!));
        dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.started", beforeJson, SerializeTask(projectTask), now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> AssignProjectMemberToTaskAsync(Guid taskId, AssignProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var projectTask = await dbContext.ProjectTasks.FindAsync([taskId], cancellationToken);
        if (projectTask is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Task"));
        var authorizationError = await GetTaskAuthorizationErrorAsync(projectTask.ProjectId, PermissionCodes.TaskAssign, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(candidate => candidate.Id == request.ProjectMemberId && candidate.ProjectId == projectTask.ProjectId && candidate.Status == MembershipStatus.Active, cancellationToken);
        if (member is null)
            return ApplicationResult.Failure(ApplicationErrors.Validation("task.invalid_assignee", "Assignee is not an active project member.", "projectMemberId"));
        var assigneeIsNew = !await dbContext.TaskAssignees.AnyAsync(assignment => assignment.TaskId == taskId && assignment.ProjectMemberId == request.ProjectMemberId, cancellationToken);
        if (assigneeIsNew)
        {
            var now = timeProvider.GetUtcNow();
            var beforeAssignmentJson = SerializeTask(projectTask);
            dbContext.TaskAssignees.Add(TaskAssignee.AssignProjectMemberToTask(taskId, request.ProjectMemberId, currentUser.UserId!.Value, now));
            dbContext.UserNotifications.Add(UserNotification.CreateUserNotification(member.UserId, "task.assigned", "Bạn có công việc mới", projectTask.Title, nameof(ProjectTask), taskId.ToString(), now));
            dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.assigned", null, JsonSerializer.Serialize(new { request.ProjectMemberId }), now));
            // Assignment is the hand-off from planning to execution. A fresh TODO task
            // therefore moves into the active column immediately; other states stay intact.
            if (projectTask.Status == PlanoraTaskStatus.Todo && projectTask.StartTask(now).IsSuccess)
                dbContext.AuditLogs.Add(CreateTaskAuditLog(projectTask, "task.started_on_assignment", beforeAssignmentJson, SerializeTask(projectTask), now));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        if (assigneeIsNew)
        {
            var payload = new { taskId = projectTask.Id, title = projectTask.Title, assigneeId = member.UserId };
            await realtimeNotifier.NotifyUserAsync(member.UserId, "TaskAssigned", payload, cancellationToken);
            await realtimeNotifier.NotifyUserAsync(member.UserId, "NotificationReceived", new { type = "task.assigned", title = "Bạn có công việc mới", message = projectTask.Title, taskId = projectTask.Id }, cancellationToken);
            await taskEmailNotificationService.QueueTaskEventEmailsAsync(
                projectTask,
                currentUser.UserId!.Value,
                [member.UserId],
                TaskEmailEvent.Assigned,
                cancellationToken);
        }
        return ApplicationResult.Success();
    }

    public async Task<int> ExpireOverdueProjectTasksAsync(CancellationToken cancellationToken)
    {
        var currentTime = timeProvider.GetUtcNow();
        var overdueTasks = await dbContext.ProjectTasks.Where(task => task.EffectiveDueAt < currentTime && (task.Status == PlanoraTaskStatus.Todo || task.Status == PlanoraTaskStatus.InProgress || task.Status == PlanoraTaskStatus.Rework)).ToListAsync(cancellationToken);
        var expiredTaskCount = 0;
        foreach (var projectTask in overdueTasks)
        {
            var hasSubmissionBeforeDeadline = await dbContext.TaskSubmissions.AnyAsync(submission => submission.TaskId == projectTask.Id && submission.SubmittedAt <= projectTask.EffectiveDueAt, cancellationToken);
            if (projectTask.ExpireTaskIfOverdue(currentTime, hasSubmissionBeforeDeadline))
            {
                expiredTaskCount++;
                dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(null, projectTask.ProjectId, "task.expired", nameof(ProjectTask), projectTask.Id.ToString(), null, SerializeTask(projectTask), null, currentTime));
            }
        }
        if (expiredTaskCount > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
        return expiredTaskCount;
    }

    private async Task<ApplicationError?> ValidateTaskRequestAsync(Guid projectId, Guid? sprintId, string title, DateTimeOffset? dueAt, Guid? dependsOnTaskId, CancellationToken cancellationToken, Guid? currentTaskId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ApplicationErrors.Validation("task.title_required", "Task title is required.", "title");
        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is null)
            return ApplicationErrors.NotFound("Project");
        if (dueAt is not null && ((project.StartAt is not null && dueAt < project.StartAt) || (project.EndAt is not null && dueAt > project.EndAt)))
            return ApplicationErrors.Validation("task.outside_project_period", "Task deadline must be inside the project period.", "dueAt");
        if (sprintId is Guid targetSprintId)
        {
            var sprint = await dbContext.Sprints.FirstOrDefaultAsync(item => item.Id == targetSprintId && item.ProjectId == projectId, cancellationToken);
            if (sprint is null)
                return ApplicationErrors.Validation("task.invalid_sprint", "Sprint does not belong to the project.", "sprintId");
            if (dueAt is not null && (dueAt < sprint.StartAt || dueAt > sprint.EndAt))
                return ApplicationErrors.Validation("task.outside_sprint_period", "Task deadline must be inside the selected sprint period.", "dueAt");
        }
        if (dependsOnTaskId is not null && dependsOnTaskId == currentTaskId)
            return ApplicationErrors.Validation("task.self_dependency", "A task cannot depend on itself.", "dependsOnTaskId");
        if (dependsOnTaskId is Guid dependencyId && !await dbContext.ProjectTasks.AnyAsync(item => item.Id == dependencyId && item.ProjectId == projectId && item.DeletedAt == null, cancellationToken))
            return ApplicationErrors.Validation("task.invalid_dependency", "Dependency must be another active task in this project.", "dependsOnTaskId");
        return null;
    }

    private async Task<bool> CurrentUserIsTaskAssigneeAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return false;
        return await (from taskAssignee in dbContext.TaskAssignees
                      join projectMember in dbContext.ProjectMembers on taskAssignee.ProjectMemberId equals projectMember.Id
                      where taskAssignee.TaskId == taskId && projectMember.UserId == currentUser.UserId && projectMember.Status == MembershipStatus.Active
                      select taskAssignee).AnyAsync(cancellationToken);
    }

    private async Task<ApplicationError?> GetTaskAuthorizationErrorAsync(Guid projectId, string permissionCode, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, permissionCode, null, cancellationToken) ? null : ApplicationErrors.NotFound("Task");
    }

    private static TaskAcceptanceCriterion[] CreateAcceptanceCriteria(Guid taskId, IReadOnlyList<string> criteria) => criteria.Where(content => !string.IsNullOrWhiteSpace(content)).Select((content, index) => TaskAcceptanceCriterion.CreateTaskAcceptanceCriterion(taskId, content, index)).ToArray();
    private static string[] NormalizeExtensions(IReadOnlyList<string>? extensions) => extensions?.Where(extension => !string.IsNullOrWhiteSpace(extension)).Select(extension => extension.Trim().TrimStart('.').ToLowerInvariant()).Distinct().Take(20).ToArray() ?? [];
    private static string? NormalizeTaskType(string? type)
    {
        var normalized = string.Join(' ', (type ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized.Length is > 0 and <= 40 ? normalized : null;
    }
    private AuditLog CreateTaskAuditLog(ProjectTask task, string action, string? beforeJson, string? afterJson, DateTimeOffset now) => AuditLog.CreateAuditLog(currentUser.UserId, task.ProjectId, action, nameof(ProjectTask), task.Id.ToString(), beforeJson, afterJson, currentUser.IpAddress, now);
    private static string SerializeTask(ProjectTask task) => JsonSerializer.Serialize(new { task.Title, task.Description, task.Type, task.Priority, task.Status, task.SprintId, task.OriginalDueAt, task.EffectiveDueAt, task.SubmissionRequirement, task.AllowedExtensionsCsv, task.DependsOnTaskId, task.IsMilestone });
    private static ProjectTaskResponse MapProjectTaskResponse(ProjectTask task, IReadOnlyList<string> acceptanceCriteria, IReadOnlyList<Guid> assigneeMemberIds) => new(task.Id, task.ProjectId, task.SprintId, task.Title, task.Description, task.Priority, task.Status, task.OriginalDueAt, task.EffectiveDueAt, acceptanceCriteria, assigneeMemberIds, task.Type, task.SubmissionRequirement, task.AllowedExtensionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), task.DependsOnTaskId, task.IsMilestone);
}
