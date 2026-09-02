using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;
using Planora.Domain.Support;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/feedback")]
public sealed class AdminFeedbackController(FeedbackAdministrationService feedbackAdministrationService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetFeedbackItemsAsync(
        [FromQuery] FeedbackStatus? status,
        [FromQuery] FeedbackPriority? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await feedbackAdministrationService.GetFeedbackItemsAsync(status, priority, page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("{feedbackId:guid}")]
    public async Task<IResult> GetFeedbackItemByIdAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var result = await feedbackAdministrationService.GetFeedbackItemByIdAsync(feedbackId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{feedbackId:guid}/assign")]
    public async Task<IResult> AssignFeedbackItemAsync(Guid feedbackId, AssignFeedbackItemRequest request, CancellationToken cancellationToken)
    {
        var result = await feedbackAdministrationService.AssignFeedbackItemAsync(feedbackId, request, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{feedbackId:guid}/resolve")]
    public async Task<IResult> ResolveFeedbackItemAsync(Guid feedbackId, ResolveFeedbackItemRequest request, CancellationToken cancellationToken)
    {
        var result = await feedbackAdministrationService.ResolveFeedbackItemAsync(feedbackId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
