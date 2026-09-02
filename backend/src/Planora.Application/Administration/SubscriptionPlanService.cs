using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;
using Planora.Domain.Projects;

namespace Planora.Application.Administration;

public sealed class SubscriptionPlanService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<IReadOnlyList<SubscriptionPlanResponse>>> GetSubscriptionPlansAsync(CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<SubscriptionPlanResponse>>(authorizationError);

        var planRows = await dbContext.SubscriptionPlans
            .OrderBy(plan => plan.Price)
            .Select(plan => new
            {
                Plan = plan,
                ActiveSubscriberCount = dbContext.UserSubscriptions.Count(subscription => subscription.PlanId == plan.Id && subscription.Status == SubscriptionStatus.Active)
            })
            .ToListAsync(cancellationToken);
        return ApplicationResult.Success<IReadOnlyList<SubscriptionPlanResponse>>(
            planRows.Select(row => MapSubscriptionPlanResponse(row.Plan, row.ActiveSubscriberCount)).ToArray());
    }

    public async Task<ApplicationResult<SubscriptionPlanResponse>> CreateSubscriptionPlanAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<SubscriptionPlanResponse>(authorizationError);
        var validationError = ValidateSubscriptionPlanRequest(request.Code, request.Name, request.Price, request.Currency, request.MaxOwnedProjects, request.MaxStorageBytes);
        if (validationError is not null)
            return ApplicationResult.Failure<SubscriptionPlanResponse>(validationError);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Code == normalizedCode, cancellationToken))
            return ApplicationResult.Failure<SubscriptionPlanResponse>(ApplicationErrors.Conflict("plan.code_exists", "A subscription plan with this code already exists."));

        var currentTime = timeProvider.GetUtcNow();
        var plan = SubscriptionPlan.CreateSubscriptionPlan(
            request.Code,
            request.Name,
            request.Price,
            request.Currency,
            request.BillingPeriod,
            request.MaxOwnedProjects,
            request.MaxStorageBytes,
            JsonSerializer.Serialize(request.Entitlements),
            currentTime);
        dbContext.SubscriptionPlans.Add(plan);
        AddPlanAuditLog("plan.created", plan.Id, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(MapSubscriptionPlanResponse(plan, 0));
    }

    public async Task<ApplicationResult<SubscriptionPlanResponse>> UpdateSubscriptionPlanAsync(
        Guid planId,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<SubscriptionPlanResponse>(authorizationError);
        var validationError = ValidateSubscriptionPlanRequest("existing", request.Name, request.Price, request.Currency, request.MaxOwnedProjects, request.MaxStorageBytes);
        if (validationError is not null)
            return ApplicationResult.Failure<SubscriptionPlanResponse>(validationError);

        var plan = await dbContext.SubscriptionPlans.FindAsync([planId], cancellationToken);
        if (plan is null)
            return ApplicationResult.Failure<SubscriptionPlanResponse>(ApplicationErrors.NotFound("Subscription plan"));

        var currentTime = timeProvider.GetUtcNow();
        plan.UpdateSubscriptionPlan(
            request.Name,
            request.Price,
            request.Currency,
            request.BillingPeriod,
            request.MaxOwnedProjects,
            request.MaxStorageBytes,
            JsonSerializer.Serialize(request.Entitlements),
            request.IsActive,
            currentTime);
        AddPlanAuditLog("plan.updated", plan.Id, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        var activeSubscriberCount = await dbContext.UserSubscriptions.CountAsync(
            subscription => subscription.PlanId == plan.Id && subscription.Status == SubscriptionStatus.Active,
            cancellationToken);
        return ApplicationResult.Success(MapSubscriptionPlanResponse(plan, activeSubscriberCount));
    }

    private static ApplicationError? ValidateSubscriptionPlanRequest(
        string code,
        string name,
        decimal price,
        string currency,
        int maxOwnedProjects,
        long maxStorageBytes)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ApplicationErrors.Validation("plan.code_required", "Plan code is required.", "code");
        if (string.IsNullOrWhiteSpace(name))
            return ApplicationErrors.Validation("plan.name_required", "Plan name is required.", "name");
        if (price < 0)
            return ApplicationErrors.Validation("plan.price_invalid", "Plan price cannot be negative.", "price");
        if (string.IsNullOrWhiteSpace(currency))
            return ApplicationErrors.Validation("plan.currency_required", "Currency is required.", "currency");
        if (maxOwnedProjects < 1)
            return ApplicationErrors.Validation("plan.project_quota_invalid", "Project quota must be at least one.", "maxOwnedProjects");
        if (maxStorageBytes < 1)
            return ApplicationErrors.Validation("plan.storage_quota_invalid", "Storage quota must be greater than zero.", "maxStorageBytes");
        return null;
    }

    private static SubscriptionPlanResponse MapSubscriptionPlanResponse(SubscriptionPlan plan, int activeSubscriberCount) => new(
        plan.Id,
        plan.Code,
        plan.Name,
        plan.Price,
        plan.Currency,
        plan.BillingPeriod,
        plan.MaxOwnedProjects,
        plan.MaxStorageBytes,
        JsonSerializer.Deserialize<string[]>(plan.EntitlementsJson) ?? [],
        plan.IsActive,
        activeSubscriberCount,
        plan.UpdatedAt);

    private void AddPlanAuditLog(string action, Guid planId, DateTimeOffset createdAt) =>
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(
            currentUser.UserId,
            null,
            action,
            nameof(SubscriptionPlan),
            planId.ToString(),
            null,
            null,
            currentUser.IpAddress,
            createdAt));
}
