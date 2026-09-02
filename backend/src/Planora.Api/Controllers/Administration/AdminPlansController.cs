using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/plans")]
public sealed class AdminPlansController(SubscriptionPlanService subscriptionPlanService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetSubscriptionPlansAsync(CancellationToken cancellationToken)
    {
        var result = await subscriptionPlanService.GetSubscriptionPlansAsync(cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost]
    public async Task<IResult> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await subscriptionPlanService.CreateSubscriptionPlanAsync(request, cancellationToken);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    [HttpPut("{planId:guid}")]
    public async Task<IResult> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await subscriptionPlanService.UpdateSubscriptionPlanAsync(planId, request, cancellationToken);
        return result.ToHttpResult();
    }
}
