using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Billing;

namespace Planora.Api.Controllers.Billing;

[ApiController]
[Authorize]
[Route("api/billing")]
public sealed class BillingController(BillingService billingService) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IResult> GetAvailablePlansAsync(CancellationToken cancellationToken) =>
        (await billingService.GetAvailablePlansAsync(cancellationToken)).ToHttpResult();

    [HttpGet("subscription")]
    public async Task<IResult> GetMySubscriptionAsync(CancellationToken cancellationToken) =>
        (await billingService.GetMySubscriptionAsync(cancellationToken)).ToHttpResult();

    [HttpGet("payments")]
    public async Task<IResult> GetMyPaymentsAsync(CancellationToken cancellationToken) =>
        (await billingService.GetMyPaymentsAsync(cancellationToken)).ToHttpResult();

    [HttpPost("payments")]
    public async Task<IResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken) =>
        (await billingService.CreatePaymentIntentAsync(request, cancellationToken)).ToHttpResult(StatusCodes.Status201Created);

    [HttpPost("subscription/cancel")]
    public async Task<IResult> CancelMySubscriptionAsync(CancellationToken cancellationToken) =>
        (await billingService.CancelMySubscriptionAsync(cancellationToken)).ToHttpResult();
}
