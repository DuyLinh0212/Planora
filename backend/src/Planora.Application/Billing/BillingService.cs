using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;

namespace Planora.Application.Billing;

public sealed class BillingService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IMomoPaymentGateway momoPaymentGateway,
    IBankTransferPaymentDetailsProvider bankTransferDetails)
{
    public async Task<ApplicationResult<IReadOnlyList<AvailablePlanResponse>>> GetAvailablePlansAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApplicationResult.Failure<IReadOnlyList<AvailablePlanResponse>>(ApplicationErrors.Unauthorized());
        await EnsureDefaultPlansAsync(cancellationToken);
        var planEntities = await dbContext.SubscriptionPlans.Where(plan => plan.IsActive).OrderBy(plan => plan.Price).ToListAsync(cancellationToken);
        var plans = planEntities.Select(plan => new AvailablePlanResponse(
            plan.Id, plan.Code, plan.Name, plan.Price, plan.Currency, plan.BillingPeriod,
            plan.MaxOwnedProjects, plan.MaxStorageBytes,
            JsonSerializer.Deserialize<string[]>(plan.EntitlementsJson) ?? [])).ToArray();
        return ApplicationResult.Success<IReadOnlyList<AvailablePlanResponse>>(plans);
    }

    public async Task<ApplicationResult<UserSubscriptionResponse>> GetMySubscriptionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<UserSubscriptionResponse>(ApplicationErrors.Unauthorized());
        var subscription = await dbContext.UserSubscriptions.Where(item => item.UserId == userId && item.Status == SubscriptionStatus.Active).OrderByDescending(item => item.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
            return ApplicationResult.Success(new UserSubscriptionResponse(null, "FREE", "Free", null, null, null, false));
        var plan = await dbContext.SubscriptionPlans.FindAsync([subscription.PlanId], cancellationToken);
        return ApplicationResult.Success(new UserSubscriptionResponse(subscription.Id, plan?.Code ?? "FREE", plan?.Name ?? "Free", subscription.Status, subscription.StartedAt, subscription.ExpiresAt, subscription.AutoRenew));
    }

    public async Task<ApplicationResult<IReadOnlyList<UserPaymentResponse>>> GetMyPaymentsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<IReadOnlyList<UserPaymentResponse>>(ApplicationErrors.Unauthorized());
        var payments = await (from payment in dbContext.PaymentTransactions
                              join plan in dbContext.SubscriptionPlans on payment.PlanId equals plan.Id
                              where payment.UserId == userId
                              orderby payment.CreatedAt descending
                              select new UserPaymentResponse(payment.Id, plan.Name, payment.Provider, payment.Amount, payment.Currency, payment.Status, payment.CreatedAt, payment.PaidAt))
            .Take(100).ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<UserPaymentResponse>>(payments);
    }

    public async Task<ApplicationResult<PaymentCheckoutResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Unauthorized());
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Validation("payment.idempotency_key_required", "Provide an idempotency key before creating a payment.", "idempotencyKey"));
        var normalizedKey = request.IdempotencyKey.Trim();
        var existing = await dbContext.PaymentTransactions.FirstOrDefaultAsync(payment => payment.UserId == userId && payment.IdempotencyKey == normalizedKey, cancellationToken);
        if (existing is not null)
        {
            var existingPlan = await dbContext.SubscriptionPlans.FindAsync([existing.PlanId], cancellationToken);
            return await CreateCheckoutResponseAsync(existing, existingPlan?.Name ?? "Plan", cancellationToken);
        }
        var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken);
        if (plan is null)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.NotFound("Active subscription plan"));
        if (plan.Price <= 0)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Validation("payment.paid_plan_required", "Choose a paid subscription plan.", "planId"));
        if (plan.Currency != "VND" || decimal.Truncate(plan.Price) != plan.Price)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Validation("payment.invalid_currency", "MoMo and bank transfer currently support whole-VND plan prices only.", "planId"));
        if (request.Provider == PaymentProvider.Momo && !momoPaymentGateway.IsConfigured)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.External("payment.momo_not_configured", "MoMo is not configured yet. Contact support or choose bank transfer."));
        if (request.Provider == PaymentProvider.BankTransfer && !bankTransferDetails.IsConfigured)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.External("payment.bank_transfer_not_configured", "Automatic bank transfer is not configured yet. Please try again later."));
        if (request.Provider is not (PaymentProvider.Momo or PaymentProvider.BankTransfer))
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Validation("payment.provider_not_available", "This payment method is not available.", "provider"));

        var payment = PaymentTransaction.CreatePaymentTransaction(userId, plan.Id, request.Provider, plan.Price, plan.Currency, normalizedKey, timeProvider.GetUtcNow());
        payment.SetProviderOrderId(request.Provider == PaymentProvider.Momo ? CreateMomoOrderId(payment.Id) : bankTransferDetails.CreatePaymentReference(payment.Id));
        dbContext.PaymentTransactions.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await CreateCheckoutResponseAsync(payment, plan.Name, cancellationToken);
    }

    public async Task<ApplicationResult> ProcessMomoPaymentCallbackAsync(MomoPaymentCallback callback, CancellationToken cancellationToken)
    {
        if (!momoPaymentGateway.IsValidCallbackSignature(callback))
            return ApplicationResult.Failure(ApplicationErrors.Validation("payment.invalid_momo_signature", "The MoMo callback signature is invalid."));

        var payment = await dbContext.PaymentTransactions.FirstOrDefaultAsync(candidate =>
            candidate.Provider == PaymentProvider.Momo && candidate.ProviderOrderId == callback.OrderId, cancellationToken);
        if (payment is null)
            return ApplicationResult.Success(); // Acknowledge an old or unknown valid callback; retries cannot recover it.
        if (payment.Status == PaymentStatus.Success)
            return ApplicationResult.Success();
        if (payment.Amount != callback.Amount || !string.Equals(payment.Currency, "VND", StringComparison.Ordinal))
            return ApplicationResult.Failure(ApplicationErrors.Validation("payment.momo_amount_mismatch", "The MoMo callback amount does not match this order."));

        if (callback.ResultCode != 0)
        {
            payment.MarkPaymentTransactionFailed(callback.TransId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }

        return await MarkPaymentSuccessfulAsync(payment, callback.TransId ?? callback.OrderId, cancellationToken);
    }

    public async Task<ApplicationResult> ProcessBankTransferWebhookAsync(BankTransferWebhook callback, CancellationToken cancellationToken)
    {
        if (!bankTransferDetails.IsExpectedDestinationAccount(callback.AccountNumber))
            return ApplicationResult.Success();
        if (!string.Equals(callback.TransferType, "in", StringComparison.OrdinalIgnoreCase) && !string.Equals(callback.TransferType, "credit", StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Success();
        var paymentReference = bankTransferDetails.ExtractPaymentReference(callback.Content);
        if (paymentReference is null)
            return ApplicationResult.Success();

        var payment = await dbContext.PaymentTransactions.FirstOrDefaultAsync(candidate =>
            candidate.Provider == PaymentProvider.BankTransfer && candidate.ProviderOrderId == paymentReference, cancellationToken);
        if (payment is null || payment.Status == PaymentStatus.Success)
            return ApplicationResult.Success();
        if (payment.Amount != callback.TransferAmount || !string.Equals(payment.Currency, "VND", StringComparison.Ordinal))
            return ApplicationResult.Success();

        return await MarkPaymentSuccessfulAsync(payment, callback.TransactionId.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
    }

    public async Task<ApplicationResult> CancelMySubscriptionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        var subscription = await dbContext.UserSubscriptions.Where(item => item.UserId == userId && item.Status == SubscriptionStatus.Active).OrderByDescending(item => item.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("subscription.not_active", "There is no active paid subscription to cancel."));
        subscription.CancelUserSubscription(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task EnsureDefaultPlansAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.SubscriptionPlans
            .Select(plan => plan.Code)
            .ToListAsync(cancellationToken);
        var knownCodes = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = timeProvider.GetUtcNow();
        var defaults = new[]
        {
            SubscriptionPlan.CreateSubscriptionPlan(
                "FREE", "Free", 0, "VND", BillingPeriod.Forever, 1, 500L * 1024 * 1024,
                JsonSerializer.Serialize(new[] { "1 project sở hữu", "500 MB lưu trữ", "5 thành viên / project", "5 phiên bản / file" }), now),
            SubscriptionPlan.CreateSubscriptionPlan(
                "PRO", "Pro", 149_000, "VND", BillingPeriod.Monthly, 10, 20L * 1024 * 1024 * 1024,
                JsonSerializer.Serialize(new[] { "10 project sở hữu", "20 GB lưu trữ", "25 thành viên / project", "30 phiên bản / file", "File tối đa 100 MB" }), now),
            SubscriptionPlan.CreateSubscriptionPlan(
                "VIP", "VIP", 399_000, "VND", BillingPeriod.Monthly, 50, 100L * 1024 * 1024 * 1024,
                JsonSerializer.Serialize(new[] { "50 project sở hữu", "100 GB lưu trữ", "Thành viên mở rộng", "Lịch sử phiên bản nâng cao", "Ưu tiên hỗ trợ" }), now)
        };

        var missingPlans = defaults.Where(plan => !knownCodes.Contains(plan.Code)).ToArray();
        if (missingPlans.Length == 0)
            return;
        dbContext.SubscriptionPlans.AddRange(missingPlans);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationResult<PaymentCheckoutResponse>> CreateCheckoutResponseAsync(PaymentTransaction payment, string planName, CancellationToken cancellationToken)
    {
        var response = ToUserPaymentResponse(payment, planName);
        if (payment.Provider == PaymentProvider.BankTransfer)
        {
            if (!bankTransferDetails.IsConfigured || string.IsNullOrWhiteSpace(payment.ProviderOrderId))
                return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.External("payment.bank_transfer_not_configured", "Automatic bank transfer is not configured yet. Please try again later."));
            return ApplicationResult.Success(new PaymentCheckoutResponse(response, null, bankTransferDetails.GetInstructions(payment.ProviderOrderId, payment.Amount)));
        }

        if (payment.Provider != PaymentProvider.Momo)
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.Validation("payment.provider_not_available", "This payment method is not available.", "provider"));
        if (!momoPaymentGateway.IsConfigured || string.IsNullOrWhiteSpace(payment.ProviderOrderId))
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.External("payment.momo_not_configured", "MoMo is not configured yet. Contact support or choose bank transfer."));
        if (payment.Status == PaymentStatus.Success || payment.Status == PaymentStatus.Failed)
            return ApplicationResult.Success(new PaymentCheckoutResponse(response, payment.CheckoutUrl, null));
        if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl))
            return ApplicationResult.Success(new PaymentCheckoutResponse(response, payment.CheckoutUrl, null));

        var checkout = await momoPaymentGateway.CreateCheckoutAsync(new MomoCheckoutRequest(
            payment.ProviderOrderId,
            payment.Amount,
            $"Planora {planName} ({payment.ProviderOrderId})"), cancellationToken);
        if (string.IsNullOrWhiteSpace(checkout.CheckoutUrl))
            return ApplicationResult.Failure<PaymentCheckoutResponse>(ApplicationErrors.External("payment.momo_unavailable", checkout.ErrorMessage ?? "MoMo is temporarily unavailable. Please retry with the same payment."));
        payment.SetCheckoutUrl(checkout.CheckoutUrl);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new PaymentCheckoutResponse(ToUserPaymentResponse(payment, planName), payment.CheckoutUrl, null));
    }

    private async Task<ApplicationResult> MarkPaymentSuccessfulAsync(PaymentTransaction payment, string providerTransactionId, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Success)
            return ApplicationResult.Success();
        var now = timeProvider.GetUtcNow();
        var plan = await dbContext.SubscriptionPlans.FindAsync([payment.PlanId], cancellationToken);
        if (plan is null || !plan.IsActive)
            return ApplicationResult.Failure(ApplicationErrors.Validation("payment.plan_unavailable", "The subscription plan is no longer available."));

        var alreadyActivated = await dbContext.UserSubscriptions.AnyAsync(subscription => subscription.PaymentTransactionId == payment.Id, cancellationToken);
        if (alreadyActivated)
            return ApplicationResult.Success();

        var activeSubscriptions = await dbContext.UserSubscriptions
            .Where(subscription => subscription.UserId == payment.UserId && subscription.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var activeSubscription in activeSubscriptions)
            activeSubscription.ExpireUserSubscription(now);

        var subscription = UserSubscription.ActivateUserSubscription(
            payment.UserId,
            plan.Id,
            payment.Id,
            now,
            GetSubscriptionExpiry(plan.BillingPeriod, now),
            autoRenew: false);
        dbContext.UserSubscriptions.Add(subscription);
        payment.MarkPaymentTransactionSucceeded(providerTransactionId, subscription.Id, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static UserPaymentResponse ToUserPaymentResponse(PaymentTransaction payment, string planName) =>
        new(payment.Id, planName, payment.Provider, payment.Amount, payment.Currency, payment.Status, payment.CreatedAt, payment.PaidAt);

    private static string CreateMomoOrderId(Guid paymentId) => $"PLN-{paymentId:N}";

    private static DateTimeOffset? GetSubscriptionExpiry(BillingPeriod billingPeriod, DateTimeOffset startedAt) => billingPeriod switch
    {
        BillingPeriod.Monthly => startedAt.AddMonths(1),
        BillingPeriod.Yearly => startedAt.AddYears(1),
        _ => null
    };
}
