using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Sprints;

namespace Planora.Api.Controllers.Planning;

[ApiController]
[Authorize]
[Route("api")]
public sealed class SprintsController(SprintService sprintService) : ControllerBase
{
    [HttpPost("projects/{projectId:guid}/sprints")]
    public async Task<IResult> CreateSprintAsync(Guid projectId, CreateSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await sprintService.CreateSprintAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpGet("projects/{projectId:guid}/sprints")]
    public async Task<IResult> GetProjectSprintsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sprintService.GetProjectSprintsAsync(projectId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("sprints/{sprintId:guid}/start")]
    public async Task<IResult> StartSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var result = await sprintService.StartSprintAsync(sprintId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPut("sprints/{sprintId:guid}")]
    public async Task<IResult> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await sprintService.UpdateSprintAsync(sprintId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpDelete("sprints/{sprintId:guid}")]
    public async Task<IResult> CancelSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var result = await sprintService.CancelSprintAsync(sprintId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("sprints/{sprintId:guid}/close")]
    public async Task<IResult> CloseSprintAsync(Guid sprintId, CancellationToken cancellationToken)
    {
        var result = await sprintService.CloseSprintAsync(sprintId, cancellationToken);
        return result.ToHttpResult();
    }
}
