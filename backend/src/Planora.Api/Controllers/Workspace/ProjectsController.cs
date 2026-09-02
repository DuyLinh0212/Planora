using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Projects;

namespace Planora.Api.Controllers.Workspace;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(ProjectService projectService) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await projectService.CreateProjectAsync(request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IResult> GetProjectsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await projectService.GetProjectsAsync(page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IResult> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await projectService.GetProjectByIdAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPatch("{projectId:guid}")]
    public async Task<IResult> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateProjectAsync(projectId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IResult> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await projectService.DeleteProjectAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }
}
