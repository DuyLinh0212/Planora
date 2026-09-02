using Planora.Domain.Common;

namespace Planora.Domain.Billing;

public sealed class UserSubscription : AuditableEntity
{
    private UserSubscription() { }

    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool AutoRenew { get; private set; }

    public static UserSubscription ActivateUserSubscription(
        Guid userId,
        Guid planId,
        Guid? paymentTransactionId,
        DateTimeOffset startedAt,
        DateTimeOffset? expiresAt,
        bool autoRenew)
    {
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = planId,
            PaymentTransactionId = paymentTransactionId,
            Status = SubscriptionStatus.Active,
            StartedAt = startedAt,
            ExpiresAt = expiresAt,
            AutoRenew = autoRenew
        };
        subscription.MarkCreated(startedAt);
        return subscription;
    }

    public void MoveUserSubscriptionToGracePeriod(DateTimeOffset updatedAt)
    {
        Status = SubscriptionStatus.GracePeriod;
        MarkUpdated(updatedAt);
    }

    public void CancelUserSubscription(DateTimeOffset cancelledAt)
    {
        Status = SubscriptionStatus.Cancelled;
        AutoRenew = false;
        ExpiresAt ??= cancelledAt;
        MarkUpdated(cancelledAt);
    }

    public void ExpireUserSubscription(DateTimeOffset expiredAt)
    {
        Status = SubscriptionStatus.Expired;
        AutoRenew = false;
        ExpiresAt ??= expiredAt;
        MarkUpdated(expiredAt);
    }
}
