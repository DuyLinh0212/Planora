using Planora.Domain.Billing;

namespace Planora.Application.Billing;

public sealed record CreatePaymentIntentRequest(Guid PlanId, PaymentProvider Provider, string IdempotencyKey);
public sealed record UserPaymentResponse(Guid Id, string PlanName, PaymentProvider Provider, decimal Amount, string Currency, PaymentStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt);
public sealed record PaymentCheckoutResponse(UserPaymentResponse Payment, string? CheckoutUrl, BankTransferInstructionsResponse? BankTransferInstructions);
public sealed record BankTransferInstructionsResponse(string BankName, string AccountName, string AccountNumber, string TransferContent, string? Branch);
public sealed record UserSubscriptionResponse(Guid? SubscriptionId, string PlanCode, string PlanName, SubscriptionStatus? Status, DateTimeOffset? StartedAt, DateTimeOffset? ExpiresAt, bool AutoRenew);
public sealed record AvailablePlanResponse(Guid Id, string Code, string Name, decimal Price, string Currency, BillingPeriod BillingPeriod, int MaxOwnedProjects, long MaxStorageBytes, IReadOnlyList<string> Entitlements);

public sealed record MomoCheckoutRequest(string OrderId, decimal Amount, string OrderInfo);
public sealed record MomoCheckoutSession(string? CheckoutUrl, string? ErrorMessage);
public sealed record MomoPaymentCallback(
    string PartnerCode,
    string RequestId,
    string OrderId,
    decimal Amount,
    string OrderInfo,
    string? OrderType,
    string? TransId,
    int ResultCode,
    string Message,
    string? PayType,
    long ResponseTime,
    string ExtraData,
    string Signature);
public sealed record BankTransferWebhook(long TransactionId, string AccountNumber, string Content, string TransferType, decimal TransferAmount, string? ReferenceCode);

public interface IMomoPaymentGateway
{
    bool IsConfigured { get; }
    Task<MomoCheckoutSession> CreateCheckoutAsync(MomoCheckoutRequest request, CancellationToken cancellationToken);
    bool IsValidCallbackSignature(MomoPaymentCallback callback);
}

public interface IBankTransferPaymentDetailsProvider
{
    bool IsConfigured { get; }
    string CreatePaymentReference(Guid paymentId);
    BankTransferInstructionsResponse GetInstructions(string transferContent);
    string? ExtractPaymentReference(string content);
    bool IsExpectedDestinationAccount(string accountNumber);
    bool IsValidWebhookAuthorization(string? authorizationHeader);
}
