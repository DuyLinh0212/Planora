namespace Planora.Application.Authorization;

public sealed record ProjectCapabilitiesResponse(IReadOnlyList<string> PermissionCodes);

public sealed record ProjectRolePermissionResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsSystemRole,
    bool IsEditable,
    IReadOnlyList<string> PermissionCodes);

public sealed record UpdateProjectRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);
