namespace Planora.Application.Profiles;

public sealed record UpdateMyProfileRequest(string DisplayName);
public sealed record UploadMyAvatarRequest(string FileName, string ContentType, long Length, Stream Content);
public sealed record UploadMyAvatarResponse(string AvatarUrl);
public sealed record UpdateMyPreferencesRequest(
    string PreferredLanguage,
    string ThemePreference,
    string TimeZoneId,
    bool? EmailTaskNotificationsEnabled = null);
public sealed record UsageQuotaResponse(
    string PlanCode,
    string PlanName,
    int OwnedProjects,
    int MaxOwnedProjects,
    long StorageBytes,
    long MaxStorageBytes,
    long MaxProjectStorageBytes,
    long MaxFileSizeBytes,
    long DailyUploadBytes,
    int DailyUploadCount,
    int MaxMembersPerProject,
    int MaxVersionsPerFile,
    DateTimeOffset? SubscriptionExpiresAt,
    bool AutoRenew);
public sealed record LinkMyGmailRequest(string Code, string RedirectUri);
public sealed record GmailLinkResponse(
    bool IsLinked,
    string? GmailAddress,
    bool IsServerConfigured,
    DateTimeOffset? LastSendFailedAt,
    string? LastSendFailureReason);
public sealed record MyProfileResponse(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string PreferredLanguage,
    string ThemePreference,
    string TimeZoneId,
    bool EmailTaskNotificationsEnabled,
    GmailLinkResponse GmailLink,
    int ParticipatingProjectCount,
    UsageQuotaResponse Quota);
