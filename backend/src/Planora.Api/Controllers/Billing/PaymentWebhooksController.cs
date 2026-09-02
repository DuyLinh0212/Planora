using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Billing;

namespace Planora.Api.Controllers.Billing;

[ApiController]
[AllowAnonymous]
[Route("api/payments")]
public sealed class PaymentWebhooksController(
    BillingService billingService,
    IBankTransferPaymentDetailsProvider bankTransferDetails) : ControllerBase
{
    [HttpPost("momo/ipn")]
    public async Task<IResult> ProcessMomoIpnAsync(MomoPaymentCallback callback, CancellationToken cancellationToken)
    {
        var result = await billingService.ProcessMomoPaymentCallbackAsync(callback, cancellationToken);
        if (result.IsFailure)
            return Results.BadRequest(new { resultCode = 1, message = "Invalid payment notification." });
        return Results.Ok(new { resultCode = 0, message = "Payment notification received." });
    }

    [HttpPost("bank-transfer/sepay/ipn")]
    public async Task<IResult> ProcessSePayIpnAsync(SePayWebhookRequest callback, CancellationToken cancellationToken)
    {
        if (!bankTransferDetails.IsValidWebhookAuthorization(Request.Headers.Authorization.ToString()))
            return Results.Unauthorized();

        var result = await billingService.ProcessBankTransferWebhookAsync(new BankTransferWebhook(
            callback.Id,
            callback.AccountNumber,
            callback.Content,
            callback.TransferType,
            callback.TransferAmount,
            callback.ReferenceCode), cancellationToken);
        return result.IsSuccess ? Results.Ok(new { success = true }) : Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    public sealed record SePayWebhookRequest(
        long Id,
        string AccountNumber,
        string Content,
        string TransferType,
        decimal TransferAmount,
        string? ReferenceCode);
}
