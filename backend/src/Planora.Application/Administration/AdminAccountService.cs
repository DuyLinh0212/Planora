using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;
using Planora.Domain.Projects;
using Planora.Domain.Users;

namespace Planora.Application.Administration;

public sealed class AdminAccountService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService adminAuthorizationService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<PagedResponse<AdminAccountResponse>>> GetAdminAccountsAsync(
        string? search,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<PagedResponse<AdminAccountResponse>>(authorizationError);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedSearch = search?.Trim().ToUpperInvariant();
        var query = dbContext.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
            query = query.Where(user => user.NormalizedEmail.Contains(normalizedSearch) || user.DisplayName.ToUpper().Contains(normalizedSearch));
        if (status is not null)
            query = query.Where(user => user.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var accounts = await query
            .OrderByDescending(user => user.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminAccountResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Status,
                user.SystemRole,
                dbContext.UserSubscriptions
                    .Where(subscription => subscription.UserId == user.Id && subscription.Status == SubscriptionStatus.Active)
                    .OrderByDescending(subscription => subscription.StartedAt)
                    .Join(dbContext.SubscriptionPlans, subscription => subscription.PlanId, plan => plan.Id, (subscription, plan) => plan.Name)
                    .FirstOrDefault(),
                user.CreatedAt,
                user.UpdatedAt,
                dbContext.Projects.Count(project => project.OwnerUserId == user.Id && project.DeletedAt == null),
                dbContext.FileVersions
                    .Where(version => dbContext.ProjectFiles
                        .Where(file => file.OwnerUserId == user.Id && file.DeletedAt == null)
                        .Select(file => file.Id)
                        .Contains(version.ProjectFileId))
                    .Sum(version => (long?)version.SizeBytes) ?? 0L))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success(new PagedResponse<AdminAccountResponse>(accounts, totalCount, page, pageSize));
    }

    public async Task<ApplicationResult<AdminAccountDetailsResponse>> GetAdminAccountByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<AdminAccountDetailsResponse>(authorizationError);

        var account = await dbContext.Users
            .Where(user => user.Id == accountId)
            .Select(user => new AdminAccountResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Status,
                user.SystemRole,
                dbContext.UserSubscriptions
                    .Where(subscription => subscription.UserId == user.Id && subscription.Status == SubscriptionStatus.Active)
                    .OrderByDescending(subscription => subscription.StartedAt)
                    .Join(dbContext.SubscriptionPlans, subscription => subscription.PlanId, plan => plan.Id, (subscription, plan) => plan.Name)
                    .FirstOrDefault(),
                user.CreatedAt,
                user.UpdatedAt,
                dbContext.Projects.Count(project => project.OwnerUserId == user.Id && project.DeletedAt == null),
                dbContext.FileVersions
                    .Where(version => dbContext.ProjectFiles
                        .Where(file => file.OwnerUserId == user.Id && file.DeletedAt == null)
                        .Select(file => file.Id)
                        .Contains(version.ProjectFileId))
                    .Sum(version => (long?)version.SizeBytes) ?? 0L))
            .FirstOrDefaultAsync(cancellationToken);
        if (account is null)
            return ApplicationResult.Failure<AdminAccountDetailsResponse>(ApplicationErrors.NotFound("Account"));

        var subscription = await dbContext.UserSubscriptions
            .Where(candidate => candidate.UserId == accountId && candidate.Status == SubscriptionStatus.Active)
            .OrderByDescending(candidate => candidate.StartedAt)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Status,
                candidate.StartedAt,
                candidate.ExpiresAt,
                Plan = dbContext.SubscriptionPlans.First(plan => plan.Id == candidate.PlanId)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var recentAdminActions = await dbContext.AuditLogs
            .Where(activity => activity.ProjectId == null && activity.EntityId == accountId.ToString())
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(10)
            .Select(activity => new AdminActivityResponse(
                activity.Id,
                activity.ActorUserId,
                dbContext.Users.Where(user => user.Id == activity.ActorUserId).Select(user => user.DisplayName).FirstOrDefault() ?? "System",
                activity.Action,
                activity.EntityType,
                activity.EntityId,
                activity.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApplicationResult.Success(new AdminAccountDetailsResponse(
            account,
            subscription?.Id,
            subscription?.Status,
            subscription?.StartedAt,
            subscription?.ExpiresAt,
            subscription?.Plan.MaxOwnedProjects ?? 0,
            subscription?.Plan.MaxStorageBytes ?? 0L,
            recentAdminActions));
    }

    public async Task<ApplicationResult> SuspendUserAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        if (currentUser.UserId == accountId)
            return ApplicationResult.Failure(ApplicationErrors.Conflict("admin.cannot_suspend_self", "Administrators cannot suspend their own account."));

        var account = await dbContext.Users.FindAsync([accountId], cancellationToken);
        if (account is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Account"));

        var currentTime = timeProvider.GetUtcNow();
        account.ChangeUserStatus(UserStatus.Suspended, currentTime);
        AddAccountAuditLog("account.suspended", accountId, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RestoreUserAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var authorizationError = await adminAuthorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);

        var account = await dbContext.Users.FindAsync([accountId], cancellationToken);
        if (account is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Account"));

        var currentTime = timeProvider.GetUtcNow();
        account.ChangeUserStatus(UserStatus.Active, currentTime);
        AddAccountAuditLog("account.restored", accountId, currentTime);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private void AddAccountAuditLog(string action, Guid accountId, DateTimeOffset createdAt) =>
        dbContext.AuditLogs.Add(AuditLog.CreateAuditLog(
            currentUser.UserId,
            null,
            action,
            nameof(User),
            accountId.ToString(),
            null,
            null,
            currentUser.IpAddress,
            createdAt));
}
