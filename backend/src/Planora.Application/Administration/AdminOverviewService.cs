using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;
using Planora.Domain.Projects;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Application.Administration;

public sealed class AdminOverviewService(
    IPlanoraDbContext dbContext,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<AdminOverviewResponse>> GetAdminOverviewAsync(CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<AdminOverviewResponse>(authorizationError);

        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await dbContext.Users.CountAsync(user => user.Status == UserStatus.Active, cancellationToken);
        var totalProjects = await dbContext.Projects.CountAsync(project => project.DeletedAt == null, cancellationToken);
        var activeProjects = await dbContext.Projects.CountAsync(project => project.DeletedAt == null && project.Status == ProjectStatus.Active, cancellationToken);
        var completedProjects = await dbContext.Projects.CountAsync(project => project.DeletedAt == null && project.Status == ProjectStatus.Completed, cancellationToken);
        var successfulPaymentRevenue = await dbContext.PaymentTransactions
            .Where(payment => payment.Status == PaymentStatus.Success)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        var processedPaymentCount = await dbContext.PaymentTransactions.CountAsync(
            payment => payment.Status != PaymentStatus.Pending,
            cancellationToken);
        var successfulPaymentCount = await dbContext.PaymentTransactions.CountAsync(
            payment => payment.Status == PaymentStatus.Success,
            cancellationToken);
        var paymentSuccessRate = processedPaymentCount == 0
            ? 0m
            : Math.Round(successfulPaymentCount * 100m / processedPaymentCount, 1);
        var aggregateStorageBytes = await dbContext.FileVersions.SumAsync(
            version => (long?)version.SizeBytes,
            cancellationToken) ?? 0L;

        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var periodStart = currentDate.AddDays(-6);
        var periodStartTimestamp = new DateTimeOffset(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var newUserDates = await dbContext.Users
            .Where(user => user.CreatedAt >= periodStartTimestamp)
            .Select(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
        var userActivationTrend = Enumerable.Range(0, 7)
            .Select(offset => periodStart.AddDays(offset))
            .Select(date => new TimeSeriesPointResponse(date, newUserDates.Count(createdAt => DateOnly.FromDateTime(createdAt.UtcDateTime) == date)))
            .ToArray();

        var projectStatusDistribution = await dbContext.Projects
            .Where(project => project.DeletedAt == null)
            .GroupBy(project => project.Status)
            .Select(group => new CategoryMetricResponse(group.Key.ToString(), group.Count()))
            .ToListAsync(cancellationToken);

        var subscriptionDistribution = await dbContext.UserSubscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active)
            .Join(dbContext.SubscriptionPlans, subscription => subscription.PlanId, plan => plan.Id, (subscription, plan) => plan.Name)
            .GroupBy(planName => planName)
            .Select(group => new CategoryMetricResponse(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var successfulPayments = await dbContext.PaymentTransactions
            .Where(payment => payment.Status == PaymentStatus.Success && payment.PaidAt >= periodStartTimestamp)
            .Select(payment => new { payment.PaidAt, payment.Amount })
            .ToListAsync(cancellationToken);
        var paymentRevenueTrend = Enumerable.Range(0, 7)
            .Select(offset => periodStart.AddDays(offset))
            .Select(date => new TimeSeriesPointResponse(
                date,
                successfulPayments.Where(payment => payment.PaidAt is not null && DateOnly.FromDateTime(payment.PaidAt.Value.UtcDateTime) == date).Sum(payment => payment.Amount)))
            .ToArray();

        var pendingPaymentCount = await dbContext.PaymentTransactions.CountAsync(payment => payment.Status == PaymentStatus.Pending, cancellationToken);
        var failedPaymentCount = await dbContext.PaymentTransactions.CountAsync(payment => payment.Status == PaymentStatus.Failed, cancellationToken);
        var suspendedAccountCount = await dbContext.Users.CountAsync(user => user.Status == UserStatus.Suspended, cancellationToken);
        var unresolvedFeedbackCount = await dbContext.FeedbackItems.CountAsync(feedback => feedback.Status != FeedbackStatus.Resolved, cancellationToken);
        AdminAttentionResponse[] needsAttention =
        [
            new("pending_payments", "Pending payments", pendingPaymentCount, "warning"),
            new("failed_payments", "Failed payments", failedPaymentCount, "critical"),
            new("suspended_accounts", "Suspended accounts", suspendedAccountCount, "critical"),
            new("unresolved_feedback", "Unresolved feedback", unresolvedFeedbackCount, "info")
        ];

        var recentAdminActivity = await GetRecentAdminActivityAsync(8, cancellationToken);
        return ApplicationResult.Success(new AdminOverviewResponse(
            totalUsers,
            activeUsers,
            totalProjects,
            activeProjects,
            completedProjects,
            successfulPaymentRevenue,
            paymentSuccessRate,
            aggregateStorageBytes,
            userActivationTrend,
            projectStatusDistribution,
            subscriptionDistribution,
            paymentRevenueTrend,
            needsAttention,
            recentAdminActivity));
    }

    public async Task<ApplicationResult<AdminAnalyticsResponse>> GetAdminAnalyticsAsync(
        DateOnly? periodStart,
        DateOnly? periodEnd,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<AdminAnalyticsResponse>(authorizationError);

        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var resolvedEnd = periodEnd ?? currentDate;
        var resolvedStart = periodStart ?? resolvedEnd.AddDays(-29);
        if (resolvedStart > resolvedEnd)
            return ApplicationResult.Failure<AdminAnalyticsResponse>(ApplicationErrors.Validation("admin.invalid_period", "Period start must be on or before period end."));

        var startTimestamp = new DateTimeOffset(resolvedStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endTimestamp = new DateTimeOffset(resolvedEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var userCreationDates = await dbContext.Users
            .Where(user => user.CreatedAt >= startTimestamp && user.CreatedAt < endTimestamp)
            .Select(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
        var newUsers = Enumerable.Range(0, resolvedEnd.DayNumber - resolvedStart.DayNumber + 1)
            .Select(offset => resolvedStart.AddDays(offset))
            .Select(date => new TimeSeriesPointResponse(date, userCreationDates.Count(createdAt => DateOnly.FromDateTime(createdAt.UtcDateTime) == date)))
            .ToArray();

        var usersByPlan = await dbContext.UserSubscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active)
            .Join(dbContext.SubscriptionPlans, subscription => subscription.PlanId, plan => plan.Id, (subscription, plan) => plan.Name)
            .GroupBy(planName => planName)
            .Select(group => new CategoryMetricResponse(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        var projectsByStatus = await dbContext.Projects
            .Where(project => project.DeletedAt == null)
            .GroupBy(project => project.Status)
            .Select(group => new CategoryMetricResponse(group.Key.ToString(), group.Count()))
            .ToListAsync(cancellationToken);
        var paymentsByStatus = await dbContext.PaymentTransactions
            .Where(payment => payment.CreatedAt >= startTimestamp && payment.CreatedAt < endTimestamp)
            .GroupBy(payment => payment.Status)
            .Select(group => new CategoryMetricResponse(group.Key.ToString(), group.Count()))
            .ToListAsync(cancellationToken);

        var fileVersions = await dbContext.FileVersions
            .Where(version => version.CreatedAt >= startTimestamp && version.CreatedAt < endTimestamp)
            .Select(version => new { version.CreatedAt, version.SizeBytes })
            .ToListAsync(cancellationToken);
        var runningStorageBytes = 0m;
        var storageGrowth = Enumerable.Range(0, resolvedEnd.DayNumber - resolvedStart.DayNumber + 1)
            .Select(offset => resolvedStart.AddDays(offset))
            .Select(date =>
            {
                runningStorageBytes += fileVersions
                    .Where(version => DateOnly.FromDateTime(version.CreatedAt.UtcDateTime) == date)
                    .Sum(version => version.SizeBytes);
                return new TimeSeriesPointResponse(date, runningStorageBytes);
            })
            .ToArray();

        return ApplicationResult.Success(new AdminAnalyticsResponse(
            resolvedStart,
            resolvedEnd,
            newUsers,
            usersByPlan,
            projectsByStatus,
            paymentsByStatus,
            storageGrowth));
    }

    public async Task<ApplicationResult<PagedResponse<AdminActivityResponse>>> GetAdminActivityAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<PagedResponse<AdminActivityResponse>>(authorizationError);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.AuditLogs.Where(activity => activity.ProjectId == null);
        var totalCount = await query.CountAsync(cancellationToken);
        var activities = await query
            .OrderByDescending(activity => activity.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(activity => new AdminActivityResponse(
                activity.Id,
                activity.ActorUserId,
                dbContext.Users.Where(user => user.Id == activity.ActorUserId).Select(user => user.DisplayName).FirstOrDefault() ?? "System",
                activity.Action,
                activity.EntityType,
                activity.EntityId,
                activity.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success(new PagedResponse<AdminActivityResponse>(activities, totalCount, page, pageSize));
    }

    private async Task<IReadOnlyList<AdminActivityResponse>> GetRecentAdminActivityAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.AuditLogs
            .Where(activity => activity.ProjectId == null)
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(take)
            .Select(activity => new AdminActivityResponse(
                activity.Id,
                activity.ActorUserId,
                dbContext.Users.Where(user => user.Id == activity.ActorUserId).Select(user => user.DisplayName).FirstOrDefault() ?? "System",
                activity.Action,
                activity.EntityType,
                activity.EntityId,
                activity.CreatedAt))
            .ToListAsync(cancellationToken);
}
