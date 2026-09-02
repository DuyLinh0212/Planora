using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Sprints;

namespace Planora.Application.Sprints;

public sealed class SprintService(IPlanoraDbContext dbContext, ICurrentUser currentUser, IProjectPermissionService projectPermissionService, TimeProvider timeProvider)
{
    public async Task<ApplicationResult<SprintResponse>> CreateSprintAsync(Guid projectId, CreateSprintRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetSprintAuthorizationErrorAsync(projectId, PermissionCodes.SprintCreate, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<SprintResponse>(authorizationError);
        if (request.StartAt >= request.EndAt)
            return ApplicationResult.Failure<SprintResponse>(ApplicationErrors.Validation("sprint.invalid_period", "Sprint start time must be before end time.", "endAt"));
        var project = await dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is null)
            return ApplicationResult.Failure<SprintResponse>(ApplicationErrors.NotFound("Project"));
        if ((project.StartAt is not null && request.StartAt < project.StartAt) || (project.EndAt is not null && request.EndAt > project.EndAt))
            return ApplicationResult.Failure<SprintResponse>(ApplicationErrors.Validation("sprint.outside_project_period", "Sprint dates must stay inside the project period.", "startAt"));
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            var sprintNumber = await dbContext.Sprints.IgnoreQueryFilters().CountAsync(sprint => sprint.ProjectId == projectId, cancellationToken) + 1;
            name = $"Sprint {sprintNumber}";
        }

        var sprint = Sprint.CreateSprint(projectId, name, request.Goal, request.StartAt, request.EndAt, currentUser.UserId!.Value, timeProvider.GetUtcNow());
        dbContext.Sprints.Add(sprint);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(MapSprintResponse(sprint));
    }

    public async Task<ApplicationResult<IReadOnlyList<SprintResponse>>> GetProjectSprintsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetSprintAuthorizationErrorAsync(projectId, PermissionCodes.SprintView, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<SprintResponse>>(authorizationError);

        var sprints = await dbContext.Sprints
            .Where(sprint => sprint.ProjectId == projectId)
            .OrderByDescending(sprint => sprint.StartAt)
            .Select(sprint => new SprintResponse(sprint.Id, sprint.ProjectId, sprint.Name, sprint.Goal, sprint.StartAt, sprint.EndAt, sprint.Status))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<SprintResponse>>(sprints);
    }

    public async Task<ApplicationResult> StartSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var sprint = await dbContext.Sprints.FindAsync([sprintId], cancellationToken);
        if (sprint is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Sprint"));

        var authorizationError = await GetSprintAuthorizationErrorAsync(sprint.ProjectId, PermissionCodes.SprintEdit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var projectAlreadyHasActiveSprint = await dbContext.Sprints.AnyAsync(candidate => candidate.ProjectId == sprint.ProjectId && candidate.Id != sprint.Id && candidate.Status == SprintStatus.Active, cancellationToken);
        var startResult = sprint.StartSprint(projectAlreadyHasActiveSprint, timeProvider.GetUtcNow());
        if (!startResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(startResult.Code!, startResult.Message!));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request, CancellationToken cancellationToken)
    {
        var sprint = await dbContext.Sprints.FindAsync([sprintId], cancellationToken);
        if (sprint is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Sprint"));
        var authorizationError = await GetSprintAuthorizationErrorAsync(sprint.ProjectId, PermissionCodes.SprintEdit, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var project = await dbContext.Projects.FindAsync([sprint.ProjectId], cancellationToken);
        if (project is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Project"));
        if ((project.StartAt is not null && request.StartAt < project.StartAt) || (project.EndAt is not null && request.EndAt > project.EndAt))
            return ApplicationResult.Failure(ApplicationErrors.Validation("sprint.outside_project_period", "Sprint dates must stay inside the project period.", "startAt"));
        var result = sprint.UpdateSprint(string.IsNullOrWhiteSpace(request.Name) ? sprint.Name : request.Name, request.Goal, request.StartAt, request.EndAt, timeProvider.GetUtcNow());
        if (!result.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(result.Code!, result.Message!));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> CancelSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var sprint = await dbContext.Sprints.FindAsync([sprintId], cancellationToken);
        if (sprint is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Sprint"));
        var authorizationError = await GetSprintAuthorizationErrorAsync(sprint.ProjectId, PermissionCodes.SprintClose, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var hasActiveTasks = await dbContext.ProjectTasks.AnyAsync(task => task.SprintId == sprintId && task.DeletedAt == null && task.Status != Domain.Tasks.PlanoraTaskStatus.Done && task.Status != Domain.Tasks.PlanoraTaskStatus.Cancelled, cancellationToken);
        if (hasActiveTasks)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("sprint.has_active_tasks", "Move or finish every active task before cancelling this sprint."));
        var result = sprint.CancelSprint(timeProvider.GetUtcNow());
        if (!result.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(result.Code!, result.Message!));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> CloseSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var sprint = await dbContext.Sprints.FindAsync([sprintId], cancellationToken);
        if (sprint is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Sprint"));

        var authorizationError = await GetSprintAuthorizationErrorAsync(sprint.ProjectId, PermissionCodes.SprintClose, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var closeResult = sprint.CloseSprint(timeProvider.GetUtcNow());
        if (!closeResult.IsSuccess)
            return ApplicationResult.Failure(ApplicationErrors.Conflict(closeResult.Code!, closeResult.Message!));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> GetSprintAuthorizationErrorAsync(Guid projectId, string permissionCode, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(currentUser.UserId.Value, projectId, permissionCode, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Sprint");
    }

    private static SprintResponse MapSprintResponse(Sprint sprint) => new(sprint.Id, sprint.ProjectId, sprint.Name, sprint.Goal, sprint.StartAt, sprint.EndAt, sprint.Status);
}
