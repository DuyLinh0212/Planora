using Planora.Domain.Common;

namespace Planora.Domain.Billing;

public sealed class SubscriptionPlan : AuditableEntity
{
    private SubscriptionPlan() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public BillingPeriod BillingPeriod { get; private set; }
    public int MaxOwnedProjects { get; private set; }
    public long MaxStorageBytes { get; private set; }
    public string EntitlementsJson { get; private set; } = "[]";
    public bool IsActive { get; private set; }

    public static SubscriptionPlan CreateSubscriptionPlan(
        string code,
        string name,
        decimal price,
        string currency,
        BillingPeriod billingPeriod,
        int maxOwnedProjects,
        long maxStorageBytes,
        string entitlementsJson,
        DateTimeOffset createdAt)
    {
        var plan = new SubscriptionPlan
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Price = price,
            Currency = currency.Trim().ToUpperInvariant(),
            BillingPeriod = billingPeriod,
            MaxOwnedProjects = maxOwnedProjects,
            MaxStorageBytes = maxStorageBytes,
            EntitlementsJson = entitlementsJson,
            IsActive = true
        };
        plan.MarkCreated(createdAt);
        return plan;
    }

    public void UpdateSubscriptionPlan(
        string name,
        decimal price,
        string currency,
        BillingPeriod billingPeriod,
        int maxOwnedProjects,
        long maxStorageBytes,
        string entitlementsJson,
        bool isActive,
        DateTimeOffset updatedAt)
    {
        Name = name.Trim();
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        BillingPeriod = billingPeriod;
        MaxOwnedProjects = maxOwnedProjects;
        MaxStorageBytes = maxStorageBytes;
        EntitlementsJson = entitlementsJson;
        IsActive = isActive;
        MarkUpdated(updatedAt);
    }
}
