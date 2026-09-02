using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Projects;

namespace Planora.Api.Controllers.Workspace;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/activity")]
public sealed class ProjectActivityController(ProjectActivityService projectActivityService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetProjectActivityAsync(Guid projectId, [FromQuery] int take = 100, CancellationToken cancellationToken = default) =>
        (await projectActivityService.GetProjectActivityAsync(projectId, take, cancellationToken)).ToHttpResult();
}
