namespace Planora.Domain.Billing;

public enum BillingPeriod
{
    Forever,
    Monthly,
    Yearly
}

public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled,
    GracePeriod
}

public enum PaymentProvider
{
    Momo,
    ZaloPay,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed
}
