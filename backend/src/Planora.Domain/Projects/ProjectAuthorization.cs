using Planora.Domain.Common;

namespace Planora.Domain.Projects;

public sealed class ProjectRole : Entity
{
    private ProjectRole() { }
    public Guid? ProjectId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public static ProjectRole CreateProjectRole(Guid? projectId, string code, string name, bool isSystemRole, DateTimeOffset createdAt) => new() { ProjectId = projectId, Code = code, Name = name, IsSystemRole = isSystemRole, CreatedAt = createdAt };
}

public sealed class Permission : Entity
{
    private Permission() { }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public static Permission CreatePermission(string code, string name, string module) => new() { Code = code, Name = name, Module = module };
}

public sealed class ProjectRolePermission
{
    private ProjectRolePermission() { }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public PermissionEffect Effect { get; private set; }
    public static ProjectRolePermission CreateProjectRolePermission(Guid roleId, Guid permissionId, PermissionEffect effect) => new() { RoleId = roleId, PermissionId = permissionId, Effect = effect };
}

public sealed class ProjectMemberRole
{
    private ProjectMemberRole() { }
    public Guid ProjectMemberId { get; private set; }
    public Guid RoleId { get; private set; }
    public static ProjectMemberRole CreateProjectMemberRole(Guid projectMemberId, Guid roleId) => new() { ProjectMemberId = projectMemberId, RoleId = roleId };
}
