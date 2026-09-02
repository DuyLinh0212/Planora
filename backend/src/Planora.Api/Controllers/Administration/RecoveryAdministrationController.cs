using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/recovery")]
public sealed class RecoveryAdministrationController(RecoveryAdministrationService recoveryService) : ControllerBase
{
    [HttpGet("workspace-items")]
    public async Task<IResult> GetDeletedWorkspaceItemsAsync(CancellationToken cancellationToken) =>
        (await recoveryService.GetDeletedWorkspaceItemsAsync(cancellationToken)).ToHttpResult();

    [HttpPost("projects/{projectId:guid}/restore")]
    public async Task<IResult> RestoreProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await recoveryService.RestoreProjectAsync(projectId, cancellationToken)).ToHttpResult();

    [HttpPost("sprints/{sprintId:guid}/restore")]
    public async Task<IResult> RestoreSprintAsync(Guid sprintId, CancellationToken cancellationToken) =>
        (await recoveryService.RestoreSprintAsync(sprintId, cancellationToken)).ToHttpResult();
}
