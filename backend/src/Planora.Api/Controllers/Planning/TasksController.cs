using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Tasks;

namespace Planora.Api.Controllers.Planning;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TasksController(TaskService taskService) : ControllerBase
{
    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<IResult> CreateProjectTaskAsync(Guid projectId, CreateProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.CreateProjectTaskAsync(projectId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<IResult> GetProjectTasksAsync(Guid projectId, [FromQuery] Guid? sprintId, CancellationToken cancellationToken)
    {
        var result = await taskService.GetProjectTasksAsync(projectId, sprintId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<IResult> GetProjectTaskByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskService.GetProjectTaskByIdAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPut("tasks/{taskId:guid}")]
    public async Task<IResult> UpdateProjectTaskAsync(Guid taskId, UpdateProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.UpdateProjectTaskAsync(taskId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IResult> DeleteProjectTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskService.DeleteProjectTaskAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("tasks/{taskId:guid}/history")]
    public async Task<IResult> GetProjectTaskHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskService.GetProjectTaskHistoryAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("tasks/{taskId:guid}/start")]
    public async Task<IResult> StartProjectTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskService.StartProjectTaskAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("tasks/{taskId:guid}/assignees")]
    public async Task<IResult> AssignProjectMemberToTaskAsync(Guid taskId, AssignProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.AssignProjectMemberToTaskAsync(taskId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
