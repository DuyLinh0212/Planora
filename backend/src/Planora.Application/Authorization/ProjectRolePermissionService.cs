using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;

namespace Planora.Application.Authorization;

public sealed class ProjectRolePermissionService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IProjectPermissionService projectPermissionService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<ProjectCapabilitiesResponse>> GetMyCapabilitiesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<ProjectCapabilitiesResponse>(ApplicationErrors.Unauthorized());
        var permissions = await projectPermissionService.GetUserPermissionCodesAsync(userId, projectId, cancellationToken);
        if (!permissions.Contains(PermissionCodes.ProjectView))
            return ApplicationResult.Failure<ProjectCapabilitiesResponse>(ApplicationErrors.NotFound("Project"));
        return ApplicationResult.Success(new ProjectCapabilitiesResponse(permissions.Order(StringComparer.Ordinal).ToArray()));
    }

    public async Task<ApplicationResult<IReadOnlyList<ProjectRolePermissionResponse>>> GetRolePermissionsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var authorizationError = await GetManageRolesAuthorizationErrorAsync(projectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<ProjectRolePermissionResponse>>(authorizationError);

        var roles = await dbContext.ProjectRoles
            .Where(role => role.ProjectId == projectId && role.Code != DefaultProjectRoleCodes.Owner)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
        var roleIds = roles.Select(role => role.Id).ToArray();
        var assignedPermissions = await (
            from rolePermission in dbContext.ProjectRolePermissions
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where roleIds.Contains(rolePermission.RoleId) && rolePermission.Effect == PermissionEffect.Allow
            select new { rolePermission.RoleId, permission.Code })
            .ToListAsync(cancellationToken);

        var response = roles.Select(role => new ProjectRolePermissionResponse(
            role.Id,
            role.Code,
            role.Name,
            role.IsSystemRole,
            role.Code is not DefaultProjectRoleCodes.Owner and not DefaultProjectRoleCodes.Leader,
            assignedPermissions.Where(item => item.RoleId == role.Id).Select(item => item.Code).Order(StringComparer.Ordinal).ToArray()))
            .ToArray();
        return ApplicationResult.Success<IReadOnlyList<ProjectRolePermissionResponse>>(response);
    }

    public async Task<ApplicationResult> UpdateRolePermissionsAsync(Guid projectId, Guid roleId, UpdateProjectRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await GetManageRolesAuthorizationErrorAsync(projectId, cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var role = await dbContext.ProjectRoles.FirstOrDefaultAsync(item => item.Id == roleId && item.ProjectId == projectId, cancellationToken);
        if (role is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Role"));
        if (role.Code is DefaultProjectRoleCodes.Owner or DefaultProjectRoleCodes.Leader)
            return ApplicationResult.Failure(ApplicationErrors.Forbidden("role.protected", "Owner and Leader permissions are protected."));

        var requestedCodes = request.PermissionCodes.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        requestedCodes.Add(PermissionCodes.ProjectView);
        if (requestedCodes.Contains(PermissionCodes.ProjectDelete))
            return ApplicationResult.Failure(ApplicationErrors.Forbidden("role.project_delete_internal", "Project deletion belongs to the internal project owner and cannot be granted through a workspace role."));
        var knownPermissions = await dbContext.Permissions.Where(permission => requestedCodes.Contains(permission.Code)).ToListAsync(cancellationToken);
        if (knownPermissions.Count != requestedCodes.Count)
            return ApplicationResult.Failure(ApplicationErrors.Validation("role.invalid_permission", "One or more permissions are invalid.", "permissionCodes"));

        var callerPermissions = await projectPermissionService.GetUserPermissionCodesAsync(currentUser.UserId!.Value, projectId, cancellationToken);
        var forbiddenCodes = requestedCodes.Where(code => !callerPermissions.Contains(code)).ToArray();
        if (forbiddenCodes.Length > 0)
            return ApplicationResult.Failure(ApplicationErrors.Forbidden("role.permission_escalation", "You cannot grant permissions that you do not have."));

        var existing = await dbContext.ProjectRolePermissions.Where(item => item.RoleId == roleId).ToListAsync(cancellationToken);
        dbContext.ProjectRolePermissions.RemoveRange(existing);
        dbContext.ProjectRolePermissions.AddRange(knownPermissions.Select(permission =>
            ProjectRolePermission.CreateProjectRolePermission(roleId, permission.Id, PermissionEffect.Allow)));
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(
            currentUser.UserId,
            projectId,
            "role.permissions_updated",
            nameof(ProjectRole),
            roleId.ToString(),
            null,
            string.Join(',', requestedCodes.Order(StringComparer.Ordinal)),
            currentUser.IpAddress,
            timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> GetManageRolesAuthorizationErrorAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationErrors.Unauthorized();
        return await projectPermissionService.UserHasPermissionAsync(userId, projectId, PermissionCodes.ProjectManageRoles, null, cancellationToken)
            ? null
            : ApplicationErrors.NotFound("Project");
    }
}
