using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Sprints;

namespace Planora.Application.Administration;

public sealed record DeletedWorkspaceItemResponse(Guid Id, string Name, string Kind, DateTimeOffset DeletedAt);

public sealed class RecoveryAdministrationService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<IReadOnlyList<DeletedWorkspaceItemResponse>>> GetDeletedWorkspaceItemsAsync(CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<DeletedWorkspaceItemResponse>>(authorizationError);

        var projects = await dbContext.Projects.IgnoreQueryFilters()
            .Where(project => project.DeletedAt != null)
            .Select(project => new DeletedWorkspaceItemResponse(project.Id, project.Name, "Project", project.DeletedAt!.Value))
            .ToListAsync(cancellationToken);
        var sprints = await dbContext.Sprints.IgnoreQueryFilters()
            .Where(sprint => sprint.DeletedAt != null)
            .Select(sprint => new DeletedWorkspaceItemResponse(sprint.Id, sprint.Name, "Sprint", sprint.DeletedAt!.Value))
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<DeletedWorkspaceItemResponse>>(projects.Concat(sprints).OrderByDescending(item => item.DeletedAt).ToList());
    }

    public async Task<ApplicationResult> RestoreProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null) return ApplicationResult.Failure(authorizationError);
        var project = await dbContext.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project?.DeletedAt is null) return ApplicationResult.Failure(ApplicationErrors.NotFound("Deleted project"));
        var now = timeProvider.GetUtcNow();
        project.RestoreProject(now);
        dbContext.AuditLogs.Add(Planora.Domain.Projects.AuditLog.CreateAuditLog(currentUser.UserId, project.Id, "project.restored", nameof(Project), project.Id.ToString(), null, null, currentUser.IpAddress, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RestoreSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null) return ApplicationResult.Failure(authorizationError);
        var sprint = await dbContext.Sprints.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == sprintId, cancellationToken);
        if (sprint?.DeletedAt is null) return ApplicationResult.Failure(ApplicationErrors.NotFound("Deleted sprint"));
        var projectIsAvailable = await dbContext.Projects.AnyAsync(project => project.Id == sprint.ProjectId && project.DeletedAt == null, cancellationToken);
        if (!projectIsAvailable) return ApplicationResult.Failure(ApplicationErrors.Conflict("recovery.project_deleted", "Restore the parent project before restoring its sprint."));
        var now = timeProvider.GetUtcNow();
        sprint.RestoreSprint(now);
        dbContext.AuditLogs.Add(Planora.Domain.Projects.AuditLog.CreateAuditLog(currentUser.UserId, sprint.ProjectId, "sprint.restored", nameof(Sprint), sprint.Id.ToString(), null, null, currentUser.IpAddress, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
