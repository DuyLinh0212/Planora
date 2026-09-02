using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Domain.Projects;
using Planora.Domain.Storage;

namespace Planora.Application.Authorization;

public interface IProjectPermissionService
{
    Task<bool> UserHasPermissionAsync(Guid userId, Guid projectId, string permissionCode, Guid? folderId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetUserPermissionCodesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
}

public sealed class ProjectPermissionService(IPlanoraDbContext dbContext) : IProjectPermissionService
{
    public async Task<IReadOnlySet<string>> GetUserPermissionCodesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var ownerUserId = await dbContext.Projects
            .Where(project => project.Id == projectId && project.DeletedAt == null)
            .Select(project => (Guid?)project.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);
        var projectMemberId = await dbContext.ProjectMembers
            .Where(member => member.ProjectId == projectId && member.UserId == userId && member.Status == MembershipStatus.Active)
            .Select(member => (Guid?)member.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (projectMemberId is null)
            return new HashSet<string>(StringComparer.Ordinal);

        var effects = await (
            from memberRole in dbContext.ProjectMemberRoles
            join rolePermission in dbContext.ProjectRolePermissions on memberRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where memberRole.ProjectMemberId == projectMemberId.Value
            select new { permission.Code, rolePermission.Effect })
            .ToListAsync(cancellationToken);

        var permissions = effects
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Any(item => item.Effect == PermissionEffect.Allow) && group.All(item => item.Effect != PermissionEffect.Deny))
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        // Ownership is an internal billing/lifecycle concept. It is deliberately
        // not exposed as a workspace role, but the creator must retain the final
        // ability to remove the project even after changing their visible role.
        if (ownerUserId == userId)
            permissions.Add(PermissionCodes.ProjectDelete);
        return permissions;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid projectId, string permissionCode, Guid? folderId, CancellationToken cancellationToken)
    {
        var ownerUserId = await dbContext.Projects
            .Where(project => project.Id == projectId && project.DeletedAt == null)
            .Select(project => (Guid?)project.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerUserId == userId && permissionCode == PermissionCodes.ProjectDelete)
            return true;

        var projectMemberId = await dbContext.ProjectMembers
            .Where(member => member.ProjectId == projectId && member.UserId == userId && member.Status == MembershipStatus.Active)
            .Select(member => (Guid?)member.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (projectMemberId is null)
            return false;

        var roleIds = dbContext.ProjectMemberRoles
            .Where(memberRole => memberRole.ProjectMemberId == projectMemberId.Value)
            .Select(memberRole => memberRole.RoleId);

        var permissionEffects = await (
            from rolePermission in dbContext.ProjectRolePermissions
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where roleIds.Contains(rolePermission.RoleId) && permission.Code == permissionCode
            select rolePermission.Effect).ToListAsync(cancellationToken);

        var roleAllowsPermission = permissionEffects.Contains(PermissionEffect.Allow) && !permissionEffects.Contains(PermissionEffect.Deny);
        if (folderId is null)
            return roleAllowsPermission;

        var folderAccessRules = await dbContext.FolderAccessRules
            .Where(rule => rule.FolderId == folderId &&
                ((rule.PrincipalType == StoragePrincipalType.Member && rule.ProjectMemberId == projectMemberId.Value) ||
                 (rule.PrincipalType == StoragePrincipalType.Role && rule.RoleId != null && roleIds.Contains(rule.RoleId.Value))))
            .ToListAsync(cancellationToken);

        if (folderAccessRules.Count == 0)
            return roleAllowsPermission;

        return permissionCode switch
        {
            PermissionCodes.FolderView or PermissionCodes.FileView or PermissionCodes.DocumentView => folderAccessRules.Any(rule => rule.CanView),
            PermissionCodes.FolderCreate => folderAccessRules.Any(rule => rule.CanCreate),
            PermissionCodes.FileUpload => folderAccessRules.Any(rule => rule.CanUpload),
            PermissionCodes.FolderEdit or PermissionCodes.FileEdit or PermissionCodes.DocumentEdit => folderAccessRules.Any(rule => rule.CanEdit),
            PermissionCodes.FolderDelete or PermissionCodes.FileDelete or PermissionCodes.DocumentDelete => folderAccessRules.Any(rule => rule.CanDelete),
            _ => roleAllowsPermission
        };
    }
}
