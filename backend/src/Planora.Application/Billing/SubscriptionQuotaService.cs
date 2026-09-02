using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;

namespace Planora.Application.Billing;

public sealed record EffectivePlanLimits(
    string PlanCode,
    int MaxOwnedProjects,
    int MaxMembersPerProject,
    long MaxProjectStorageBytes,
    long MaxTotalStorageBytes,
    long MaxFileSizeBytes,
    long DailyUploadBytes,
    int DailyUploadCount,
    int MaxVersionsPerFile,
    int MaxCustomRoles);

public sealed class SubscriptionQuotaService(IPlanoraDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<EffectivePlanLimits> GetEffectivePlanLimitsAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var plan = await (from subscription in dbContext.UserSubscriptions
                          join candidatePlan in dbContext.SubscriptionPlans on subscription.PlanId equals candidatePlan.Id
                          where subscription.UserId == ownerUserId && subscription.Status == SubscriptionStatus.Active && candidatePlan.IsActive
                          orderby subscription.StartedAt descending
                          select candidatePlan).FirstOrDefaultAsync(cancellationToken);
        var code = plan?.Code.ToUpperInvariant() ?? "FREE";
        var tier = code switch
        {
            "VIP" => new EffectivePlanLimits("VIP", 50, 100, 20L * 1024 * 1024 * 1024, 100L * 1024 * 1024 * 1024, 250L * 1024 * 1024, 10L * 1024 * 1024 * 1024, 1000, 100, int.MaxValue),
            "PRO" => new EffectivePlanLimits("PRO", 10, 25, 5L * 1024 * 1024 * 1024, 20L * 1024 * 1024 * 1024, 100L * 1024 * 1024, 2L * 1024 * 1024 * 1024, 300, 30, 5),
            _ => new EffectivePlanLimits("FREE", 1, 5, 500L * 1024 * 1024, 500L * 1024 * 1024, 25L * 1024 * 1024, 200L * 1024 * 1024, 50, 5, 0)
        };
        return plan is null ? tier : tier with { MaxOwnedProjects = plan.MaxOwnedProjects, MaxTotalStorageBytes = plan.MaxStorageBytes };
    }

    public async Task<ApplicationError?> GetOwnedProjectQuotaErrorAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var limits = await GetEffectivePlanLimitsAsync(ownerUserId, cancellationToken);
        var count = await dbContext.Projects.CountAsync(project => project.OwnerUserId == ownerUserId && project.DeletedAt == null, cancellationToken);
        return count >= limits.MaxOwnedProjects
            ? ApplicationErrors.Conflict("quota.owned_projects_reached", $"The {limits.PlanCode} plan allows {limits.MaxOwnedProjects} owned project(s). Upgrade or delete an owned project before creating another.")
            : null;
    }

    public async Task<ApplicationError?> GetProjectMemberQuotaErrorAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var ownerUserId = await dbContext.Projects.Where(project => project.Id == projectId).Select(project => (Guid?)project.OwnerUserId).FirstOrDefaultAsync(cancellationToken);
        if (ownerUserId is null)
            return ApplicationErrors.NotFound("Project");
        var limits = await GetEffectivePlanLimitsAsync(ownerUserId.Value, cancellationToken);
        var memberCount = await dbContext.ProjectMembers.CountAsync(member => member.ProjectId == projectId && member.Status == Domain.Projects.MembershipStatus.Active, cancellationToken);
        return memberCount >= limits.MaxMembersPerProject
            ? ApplicationErrors.Conflict("quota.project_members_reached", $"The project owner's {limits.PlanCode} plan allows {limits.MaxMembersPerProject} active members per project.")
            : null;
    }

    public async Task<ApplicationError?> GetUploadQuotaErrorAsync(Guid projectId, long incomingBytes, CancellationToken cancellationToken)
    {
        var ownerUserId = await dbContext.Projects.Where(project => project.Id == projectId).Select(project => (Guid?)project.OwnerUserId).FirstOrDefaultAsync(cancellationToken);
        if (ownerUserId is null)
            return ApplicationErrors.NotFound("Project");
        var limits = await GetEffectivePlanLimitsAsync(ownerUserId.Value, cancellationToken);
        if (incomingBytes > limits.MaxFileSizeBytes)
            return ApplicationErrors.Validation("quota.file_too_large", $"The project owner's {limits.PlanCode} plan allows files up to {limits.MaxFileSizeBytes / 1024 / 1024} MB.", "file");

        var ownedProjectIds = await dbContext.Projects.Where(project => project.OwnerUserId == ownerUserId && project.DeletedAt == null).Select(project => project.Id).ToArrayAsync(cancellationToken);
        var ownedFileIds = await dbContext.ProjectFiles.Where(file => ownedProjectIds.Contains(file.ProjectId) && file.DeletedAt == null).Select(file => file.Id).ToArrayAsync(cancellationToken);
        var projectFileIds = await dbContext.ProjectFiles.Where(file => file.ProjectId == projectId && file.DeletedAt == null).Select(file => file.Id).ToArrayAsync(cancellationToken);
        var totalBytes = await dbContext.FileVersions.Where(version => ownedFileIds.Contains(version.ProjectFileId)).SumAsync(version => (long?)version.SizeBytes, cancellationToken) ?? 0;
        var projectBytes = await dbContext.FileVersions.Where(version => projectFileIds.Contains(version.ProjectFileId)).SumAsync(version => (long?)version.SizeBytes, cancellationToken) ?? 0;
        if (totalBytes + incomingBytes > limits.MaxTotalStorageBytes)
            return ApplicationErrors.Conflict("quota.total_storage_reached", "The project owner does not have enough total storage for this upload.");
        if (projectBytes + incomingBytes > limits.MaxProjectStorageBytes)
            return ApplicationErrors.Conflict("quota.project_storage_reached", "This project does not have enough storage for the upload.");

        var now = timeProvider.GetUtcNow();
        var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var dailyVersions = dbContext.FileVersions.Where(version => ownedFileIds.Contains(version.ProjectFileId) && version.CreatedAt >= dayStart && version.CreatedAt < dayStart.AddDays(1));
        var dailyBytes = await dailyVersions.SumAsync(version => (long?)version.SizeBytes, cancellationToken) ?? 0;
        var dailyCount = await dailyVersions.CountAsync(cancellationToken);
        if (dailyBytes + incomingBytes > limits.DailyUploadBytes)
            return ApplicationErrors.Conflict("quota.daily_upload_bytes_reached", "The project owner's daily upload allowance has been reached.");
        if (dailyCount >= limits.DailyUploadCount)
            return ApplicationErrors.Conflict("quota.daily_upload_count_reached", "The project owner's daily file count has been reached.");
        return null;
    }

    public async Task<ApplicationError?> GetFileVersionQuotaErrorAsync(Guid projectId, Guid projectFileId, CancellationToken cancellationToken)
    {
        var ownerUserId = await dbContext.Projects.Where(project => project.Id == projectId).Select(project => (Guid?)project.OwnerUserId).FirstOrDefaultAsync(cancellationToken);
        if (ownerUserId is null)
            return ApplicationErrors.NotFound("Project");
        var limits = await GetEffectivePlanLimitsAsync(ownerUserId.Value, cancellationToken);
        var versionCount = await dbContext.FileVersions.CountAsync(version => version.ProjectFileId == projectFileId, cancellationToken);
        return versionCount >= limits.MaxVersionsPerFile
            ? ApplicationErrors.Conflict("quota.file_versions_reached", $"The {limits.PlanCode} plan allows {limits.MaxVersionsPerFile} versions per file.")
            : null;
    }
}
