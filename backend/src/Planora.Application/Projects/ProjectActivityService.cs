using Microsoft.EntityFrameworkCore;
using Planora.Application.Authorization;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Application.Projects;

public sealed class ProjectActivityService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService)
{
    public async Task<ApplicationResult<IReadOnlyList<ProjectActivityResponse>>> GetProjectActivityAsync(
        Guid projectId,
        int take,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<IReadOnlyList<ProjectActivityResponse>>(ApplicationErrors.Unauthorized());
        if (!await projectPermissionService.UserHasPermissionAsync(userId, projectId, PermissionCodes.ProjectView, null, cancellationToken))
            return ApplicationResult.Failure<IReadOnlyList<ProjectActivityResponse>>(ApplicationErrors.NotFound("Project"));

        var activity = await (
            from log in dbContext.AuditLogs
            join actor in dbContext.Users on log.ActorUserId equals actor.Id into actors
            from actor in actors.DefaultIfEmpty()
            where log.ProjectId == projectId
            orderby log.CreatedAt descending
            select new ProjectActivityResponse(
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.ActorUserId,
                actor == null ? "Hệ thống" : actor.DisplayName,
                log.CreatedAt))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success<IReadOnlyList<ProjectActivityResponse>>(activity);
    }
}
