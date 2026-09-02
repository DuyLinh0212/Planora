using Planora.Domain.Common;

namespace Planora.Domain.Users;

public sealed class User : AuditableEntity
{
    private User() { }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string PreferredLanguage { get; private set; } = "vi";
    public string ThemePreference { get; private set; } = "calm";
    public string TimeZoneId { get; private set; } = "Asia/Bangkok";
    public bool EmailTaskNotificationsEnabled { get; private set; }
    public DateTimeOffset? TermsAcceptedAt { get; private set; }
    public UserStatus Status { get; private set; }
    public SystemRole SystemRole { get; private set; }

    public static User CreateUser(string email, string normalizedEmail, string displayName, DateTimeOffset createdAt)
    {
        var username = email.Split('@', 2)[0];
        return CreateUser(email, normalizedEmail, username, username.ToUpperInvariant(), displayName, null, createdAt);
    }

    public static User CreateUser(
        string email,
        string normalizedEmail,
        string username,
        string normalizedUsername,
        string displayName,
        DateTimeOffset? termsAcceptedAt,
        DateTimeOffset createdAt)
    {
        var user = new User
        {
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            Username = username.Trim(),
            NormalizedUsername = normalizedUsername,
            DisplayName = displayName.Trim(),
            TermsAcceptedAt = termsAcceptedAt,
            Status = UserStatus.Active,
            SystemRole = SystemRole.User
        };
        user.MarkCreated(createdAt);
        return user;
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset updatedAt)
    {
        PasswordHash = passwordHash;
        MarkUpdated(updatedAt);
    }

    public void UpdateUserProfile(string displayName, string? avatarUrl, DateTimeOffset updatedAt)
    {
        DisplayName = displayName.Trim();
        AvatarUrl = avatarUrl;
        MarkUpdated(updatedAt);
    }

    public void UpdateUserPreferences(string preferredLanguage, string themePreference, string timeZoneId, DateTimeOffset updatedAt)
    {
        PreferredLanguage = preferredLanguage.Trim().ToLowerInvariant();
        ThemePreference = themePreference.Trim().ToLowerInvariant();
        TimeZoneId = timeZoneId.Trim();
        MarkUpdated(updatedAt);
    }

    public void UpdateUserPreferences(
        string preferredLanguage,
        string themePreference,
        string timeZoneId,
        bool emailTaskNotificationsEnabled,
        DateTimeOffset updatedAt)
    {
        EmailTaskNotificationsEnabled = emailTaskNotificationsEnabled;
        UpdateUserPreferences(preferredLanguage, themePreference, timeZoneId, updatedAt);
    }

    public void ChangeUserStatus(UserStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        MarkUpdated(updatedAt);
    }

    public void AssignSystemRole(SystemRole systemRole, DateTimeOffset updatedAt)
    {
        SystemRole = systemRole;
        MarkUpdated(updatedAt);
    }
}
