using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Billing;

namespace Planora.Application.Profiles;

public sealed class ProfileService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    GmailLinkService gmailLinkService,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<MyProfileResponse>> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<MyProfileResponse>(ApplicationErrors.Unauthorized());

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure<MyProfileResponse>(ApplicationErrors.NotFound("User profile"));

        var participatingProjectCount = await dbContext.ProjectMembers.CountAsync(
            member => member.UserId == userId && member.Status == Domain.Projects.MembershipStatus.Active,
            cancellationToken);
        var ownedProjectIds = await dbContext.Projects
            .Where(project => project.OwnerUserId == userId && project.DeletedAt == null)
            .Select(project => project.Id)
            .ToArrayAsync(cancellationToken);
        var quota = await GetUsageQuotaAsync(userId, ownedProjectIds, cancellationToken);
        var gmailLink = await gmailLinkService.GetMyGmailLinkAsync(cancellationToken);

        return ApplicationResult.Success(new MyProfileResponse(
            user.Id,
            user.Email,
            user.Username,
            user.DisplayName,
            user.AvatarUrl,
            user.PreferredLanguage,
            user.ThemePreference,
            user.TimeZoneId,
            user.EmailTaskNotificationsEnabled,
            gmailLink.Value!,
            participatingProjectCount,
            quota));
    }

    public async Task<ApplicationResult> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return ApplicationResult.Failure(ApplicationErrors.Validation("profile.display_name_required", "Display name is required.", "displayName"));
        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("User profile"));
        user.UpdateUserProfile(request.DisplayName, user.AvatarUrl, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<UploadMyAvatarResponse>> UploadMyAvatarAsync(UploadMyAvatarRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.Unauthorized());
        if (request.Length <= 0)
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.Validation("profile.avatar_required", "Choose an avatar image first.", "file"));
        if (request.Length > 5L * 1024 * 1024)
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.Validation("profile.avatar_too_large", "Avatar must be 5 MB or smaller.", "file"));
        if (!request.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.Validation("profile.avatar_type_invalid", "Avatar must be an image file.", "file"));

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.NotFound("User profile"));

        var upload = await fileStorage.UploadAvatarAsync(userId, request.FileName, request.ContentType, request.Content, cancellationToken);
        if (!upload.IsSuccess)
            return ApplicationResult.Failure<UploadMyAvatarResponse>(upload.Errors.ToArray());
        var storedAvatar = upload.Value;
        if (storedAvatar is null || string.IsNullOrWhiteSpace(storedAvatar.Url))
            return ApplicationResult.Failure<UploadMyAvatarResponse>(ApplicationErrors.External("storage.avatar_url_missing", "Avatar storage is not configured."));

        user.UpdateUserProfile(user.DisplayName, storedAvatar.Url, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(new UploadMyAvatarResponse(storedAvatar.Url));
    }

    public async Task<ApplicationResult> UpdateMyPreferencesAsync(UpdateMyPreferencesRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        var language = request.PreferredLanguage.Trim().ToLowerInvariant();
        var theme = request.ThemePreference.Trim().ToLowerInvariant();
        if (language is not ("vi" or "en"))
            return ApplicationResult.Failure(ApplicationErrors.Validation("preferences.language_invalid", "Language must be 'vi' or 'en'.", "preferredLanguage"));
        if (theme is not ("light" or "dark" or "calm"))
            return ApplicationResult.Failure(ApplicationErrors.Validation("preferences.theme_invalid", "Theme must be 'light', 'dark', or 'calm'.", "themePreference"));
        if (string.IsNullOrWhiteSpace(request.TimeZoneId))
            return ApplicationResult.Failure(ApplicationErrors.Validation("preferences.time_zone_required", "Time zone is required.", "timeZoneId"));

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("User profile"));
        var emailTaskNotificationsEnabled = request.EmailTaskNotificationsEnabled ?? user.EmailTaskNotificationsEnabled;
        if (emailTaskNotificationsEnabled && !await dbContext.UserGmailLinks.AnyAsync(link => link.UserId == userId, cancellationToken))
            return ApplicationResult.Failure(ApplicationErrors.Validation(
                "preferences.gmail_link_required",
                "Link your Gmail account before enabling email notifications.",
                "emailTaskNotificationsEnabled"));
        user.UpdateUserPreferences(
            language,
            theme,
            request.TimeZoneId,
            emailTaskNotificationsEnabled,
            timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<UsageQuotaResponse> GetUsageQuotaAsync(Guid userId, Guid[] ownedProjectIds, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.UserSubscriptions
            .Where(item => item.UserId == userId && item.Status == SubscriptionStatus.Active)
            .OrderByDescending(item => item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var plan = subscription is null ? null : await dbContext.SubscriptionPlans.FindAsync([subscription.PlanId], cancellationToken);
        var code = plan?.Code.ToUpperInvariant() ?? "FREE";
        var limits = code switch
        {
            "VIP" => (ProjectStorage: 20L * 1024 * 1024 * 1024, File: 250L * 1024 * 1024, DailyBytes: 10L * 1024 * 1024 * 1024, DailyCount: 1000, Members: 100, Versions: 100),
            "PRO" => (ProjectStorage: 5L * 1024 * 1024 * 1024, File: 100L * 1024 * 1024, DailyBytes: 2L * 1024 * 1024 * 1024, DailyCount: 300, Members: 25, Versions: 30),
            _ => (ProjectStorage: 500L * 1024 * 1024, File: 25L * 1024 * 1024, DailyBytes: 200L * 1024 * 1024, DailyCount: 50, Members: 5, Versions: 5)
        };
        var maxOwnedProjects = plan?.MaxOwnedProjects ?? 1;
        var maxStorageBytes = plan?.MaxStorageBytes ?? 500L * 1024 * 1024;
        var projectFileIds = await dbContext.ProjectFiles
            .Where(file => ownedProjectIds.Contains(file.ProjectId) && file.DeletedAt == null)
            .Select(file => file.Id)
            .ToArrayAsync(cancellationToken);
        var storageBytes = await dbContext.FileVersions
            .Where(version => projectFileIds.Contains(version.ProjectFileId))
            .SumAsync(version => (long?)version.SizeBytes, cancellationToken) ?? 0;

        return new UsageQuotaResponse(
            code,
            plan?.Name ?? "Free",
            ownedProjectIds.Length,
            maxOwnedProjects,
            storageBytes,
            maxStorageBytes,
            limits.ProjectStorage,
            limits.File,
            limits.DailyBytes,
            limits.DailyCount,
            limits.Members,
            limits.Versions,
            subscription?.ExpiresAt,
            subscription?.AutoRenew ?? false);
    }
}
