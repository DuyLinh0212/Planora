using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.ProjectMembers;

namespace Planora.Api.Controllers.Workspace;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ProjectMembersController(ProjectMemberService projectMemberService) : ControllerBase
{
    [HttpPost("projects/{projectId:guid}/invitations")]
    public async Task<IResult> InviteProjectMemberAsync(Guid projectId, InviteProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.InviteProjectMemberAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("project-invitations/{invitationId:guid}/accept")]
    public async Task<IResult> AcceptProjectInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.AcceptProjectInvitationAsync(invitationId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("project-invitations/{invitationId:guid}/reject")]
    public async Task<IResult> RejectProjectInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.RejectProjectInvitationAsync(invitationId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("projects/{projectId:guid}/members")]
    public async Task<IResult> GetProjectMembersAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.GetProjectMembersAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("projects/{projectId:guid}/member-lookup")]
    public async Task<IResult> FindRegisteredUsersAsync(Guid projectId, [FromQuery] string query, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.FindRegisteredUsersAsync(projectId, query, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("projects/{projectId:guid}/invitations")]
    public async Task<IResult> GetProjectInvitationsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.GetProjectInvitationsAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("projects/{projectId:guid}/roles")]
    public async Task<IResult> GetProjectRolesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.GetProjectRolesAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPut("projects/{projectId:guid}/members/{membershipId:guid}/role")]
    public async Task<IResult> ChangeProjectMemberRoleAsync(Guid projectId, Guid membershipId, ChangeProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.ChangeProjectMemberRoleAsync(projectId, membershipId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpDelete("projects/{projectId:guid}/members/{membershipId:guid}")]
    public async Task<IResult> RemoveProjectMemberAsync(Guid projectId, Guid membershipId, RemoveProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await projectMemberService.RemoveProjectMemberAsync(projectId, membershipId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
