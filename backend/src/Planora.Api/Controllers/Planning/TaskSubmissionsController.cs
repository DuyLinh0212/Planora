using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.TaskSubmissions;

namespace Planora.Api.Controllers.Planning;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TaskSubmissionsController(TaskSubmissionService taskSubmissionService) : ControllerBase
{
    [HttpGet("tasks/{taskId:guid}/submissions/latest")]
    public async Task<IResult> GetLatestTaskSubmissionAsync(Guid taskId, CancellationToken cancellationToken) =>
        (await taskSubmissionService.GetLatestTaskSubmissionAsync(taskId, cancellationToken)).ToHttpResult();

    [HttpPost("tasks/{taskId:guid}/submit")]
    public async Task<IResult> SubmitProjectTaskAsync(Guid taskId, SubmitProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskSubmissionService.SubmitProjectTaskAsync(taskId, request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPost("submissions/{submissionId:guid}/approve")]
    public async Task<IResult> ApproveTaskSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await taskSubmissionService.ApproveTaskSubmissionAsync(submissionId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("submissions/{submissionId:guid}/request-rework")]
    public async Task<IResult> RequestTaskSubmissionReworkAsync(Guid submissionId, ReviewTaskSubmissionRequest request, CancellationToken cancellationToken)
    {
        var result = await taskSubmissionService.RequestTaskSubmissionReworkAsync(submissionId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
