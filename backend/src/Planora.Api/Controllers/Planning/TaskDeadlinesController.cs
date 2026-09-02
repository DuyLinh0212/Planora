using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.TaskDeadlines;

namespace Planora.Api.Controllers.Planning;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TaskDeadlinesController(TaskDeadlineService taskDeadlineService) : ControllerBase
{
    [HttpGet("tasks/{taskId:guid}/deadline-history")]
    public async Task<IResult> GetTaskDeadlineHistoryAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.GetTaskDeadlineHistoryAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("tasks/{taskId:guid}/extension-requests")]
    public async Task<IResult> GetTaskExtensionRequestsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.GetTaskExtensionRequestsAsync(taskId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("tasks/{taskId:guid}/extension-requests")]
    public async Task<IResult> RequestTaskDeadlineExtensionAsync(Guid taskId, RequestTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.RequestTaskDeadlineExtensionAsync(taskId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("extension-requests/{extensionRequestId:guid}/approve")]
    public async Task<IResult> ApproveTaskDeadlineExtensionAsync(Guid extensionRequestId, ReviewTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.ApproveTaskDeadlineExtensionAsync(extensionRequestId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("extension-requests/{extensionRequestId:guid}/reject")]
    public async Task<IResult> RejectTaskDeadlineExtensionAsync(Guid extensionRequestId, ReviewTaskDeadlineExtensionRequest request, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.RejectTaskDeadlineExtensionAsync(extensionRequestId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("tasks/{taskId:guid}/extend-deadline")]
    public async Task<IResult> ExtendTaskDeadlineAsync(Guid taskId, ExtendTaskDeadlineRequest request, CancellationToken cancellationToken)
    {
        var result = await taskDeadlineService.ExtendTaskDeadlineAsync(taskId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
