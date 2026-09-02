using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;
using Planora.Domain.Projects;

namespace Planora.Application.Administration;

public sealed class PaymentAdministrationService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<PagedResponse<PaymentTransactionResponse>>> GetPaymentTransactionsAsync(
        PaymentProvider? provider,
        PaymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<PagedResponse<PaymentTransactionResponse>>(authorizationError);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.PaymentTransactions.AsQueryable();
        if (provider is not null)
            query = query.Where(payment => payment.Provider == provider);
        if (status is not null)
            query = query.Where(payment => payment.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var payments = await ProjectPaymentTransactions(query)
            .OrderByDescending(payment => payment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success(new PagedResponse<PaymentTransactionResponse>(payments, totalCount, page, pageSize));
    }

    public async Task<ApplicationResult<PaymentTransactionResponse>> GetPaymentTransactionByIdAsync(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<PaymentTransactionResponse>(authorizationError);

        var payment = await ProjectPaymentTransactions(dbContext.PaymentTransactions.Where(candidate => candidate.Id == paymentTransactionId))
            .FirstOrDefaultAsync(cancellationToken);
        return payment is null
            ? ApplicationResult.Failure<PaymentTransactionResponse>(ApplicationErrors.NotFound("Payment transaction"))
            : ApplicationResult.Success(payment);
    }

    public async Task<ApplicationResult> MarkPaymentTransactionReviewedAsync(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var payment = await dbContext.PaymentTransactions.FindAsync([paymentTransactionId], cancellationToken);
        if (payment is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Payment transaction"));

        var administratorUserId = currentUser.UserId!.Value;
        var currentTime = timeProvider.GetUtcNow();
        payment.MarkPaymentTransactionReviewed(administratorUserId, currentTime);
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(
            administratorUserId,
            null,
            "payment.reviewed",
            nameof(PaymentTransaction),
            payment.Id.ToString(),
            null,
            null,
            currentUser.IpAddress,
            currentTime));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private IQueryable<PaymentTransactionResponse> ProjectPaymentTransactions(IQueryable<PaymentTransaction> query) =>
        query.Select(payment => new PaymentTransactionResponse(
            payment.Id,
            payment.UserId,
            dbContext.Users.Where(user => user.Id == payment.UserId).Select(user => user.Email).First(),
            payment.PlanId,
            dbContext.SubscriptionPlans.Where(plan => plan.Id == payment.PlanId).Select(plan => plan.Name).First(),
            payment.Provider,
            payment.ProviderTransactionId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.IdempotencyKey,
            payment.CreatedAt,
            payment.PaidAt,
            payment.ReviewedAt));
}
