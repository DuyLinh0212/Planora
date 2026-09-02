using Planora.Domain.Common;

namespace Planora.Domain.Billing;

public sealed class PaymentTransaction : Entity
{
    private PaymentTransaction() { }

    public Guid UserId { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public Guid PlanId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string? ProviderOrderId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    public static PaymentTransaction CreatePaymentTransaction(
        Guid userId,
        Guid planId,
        PaymentProvider provider,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        PlanId = planId,
        Provider = provider,
        Amount = amount,
        Currency = currency.Trim().ToUpperInvariant(),
        Status = PaymentStatus.Pending,
        IdempotencyKey = idempotencyKey,
        CreatedAt = createdAt
    };

    public void MarkPaymentTransactionSucceeded(string providerTransactionId, Guid subscriptionId, DateTimeOffset paidAt)
    {
        ProviderTransactionId = providerTransactionId;
        SubscriptionId = subscriptionId;
        Status = PaymentStatus.Success;
        PaidAt = paidAt;
    }

    public void SetProviderOrderId(string providerOrderId)
    {
        if (string.IsNullOrWhiteSpace(providerOrderId))
            throw new ArgumentException("A provider order ID is required.", nameof(providerOrderId));
        ProviderOrderId = providerOrderId.Trim();
    }

    public void SetCheckoutUrl(string checkoutUrl)
    {
        if (string.IsNullOrWhiteSpace(checkoutUrl))
            throw new ArgumentException("A checkout URL is required.", nameof(checkoutUrl));
        CheckoutUrl = checkoutUrl.Trim();
    }

    public void MarkPaymentTransactionFailed(string? providerTransactionId)
    {
        ProviderTransactionId = providerTransactionId;
        Status = PaymentStatus.Failed;
    }

    public void MarkPaymentTransactionReviewed(Guid administratorUserId, DateTimeOffset reviewedAt)
    {
        ReviewedByUserId = administratorUserId;
        ReviewedAt = reviewedAt;
    }
}
