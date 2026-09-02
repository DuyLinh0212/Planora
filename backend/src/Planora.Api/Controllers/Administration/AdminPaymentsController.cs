using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Extensions;
using Planora.Application.Administration;
using Planora.Domain.Billing;

namespace Planora.Api.Controllers.Administration;

[ApiController]
[Authorize]
[Route("api/admin/payments")]
public sealed class AdminPaymentsController(PaymentAdministrationService paymentAdministrationService) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetPaymentTransactionsAsync(
        [FromQuery] PaymentProvider? provider,
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentAdministrationService.GetPaymentTransactionsAsync(provider, status, page, pageSize, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("{paymentTransactionId:guid}")]
    public async Task<IResult> GetPaymentTransactionByIdAsync(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var result = await paymentAdministrationService.GetPaymentTransactionByIdAsync(paymentTransactionId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{paymentTransactionId:guid}/mark-reviewed")]
    public async Task<IResult> MarkPaymentTransactionReviewedAsync(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var result = await paymentAdministrationService.MarkPaymentTransactionReviewedAsync(paymentTransactionId, cancellationToken);
        return result.ToHttpResult();
    }
}
