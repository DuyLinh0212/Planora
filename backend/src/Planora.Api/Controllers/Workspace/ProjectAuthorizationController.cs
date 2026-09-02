using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Authorization;

namespace Planora.Api.Controllers.Workspace;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}")]
public sealed class ProjectAuthorizationController(ProjectRolePermissionService permissionService) : ControllerBase
{
    [HttpGet("capabilities")]
    public async Task<IResult> GetMyCapabilitiesAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await permissionService.GetMyCapabilitiesAsync(projectId, cancellationToken)).ToHttpResult();

    [HttpGet("role-permissions")]
    public async Task<IResult> GetRolePermissionsAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await permissionService.GetRolePermissionsAsync(projectId, cancellationToken)).ToHttpResult();

    [HttpPut("roles/{roleId:guid}/permissions")]
    public async Task<IResult> UpdateRolePermissionsAsync(Guid projectId, Guid roleId, UpdateProjectRolePermissionsRequest request, CancellationToken cancellationToken) =>
        (await permissionService.UpdateRolePermissionsAsync(projectId, roleId, request, cancellationToken)).ToHttpResult();
}
