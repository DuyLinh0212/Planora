using Planora.Domain.Users;

namespace Planora.UnitTests;

public sealed class UserPreferenceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 8, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewUser_DoesNotReceiveTaskEmailBeforeLinkingGmail()
    {
        var user = CreateUser();

        Assert.False(user.EmailTaskNotificationsEnabled);
    }

    [Fact]
    public void UpdateUserPreferences_WithEmailNotificationsDisabled_StoresOptOut()
    {
        var user = CreateUser();

        user.UpdateUserPreferences("en", "dark", "UTC", emailTaskNotificationsEnabled: false, CurrentTime.AddMinutes(5));

        Assert.False(user.EmailTaskNotificationsEnabled);
        Assert.Equal("en", user.PreferredLanguage);
        Assert.Equal("dark", user.ThemePreference);
        Assert.Equal("UTC", user.TimeZoneId);
        Assert.Equal(CurrentTime.AddMinutes(5), user.UpdatedAt);
    }

    [Fact]
    public void UpdateUserPreferences_WithoutEmailArgument_KeepsExistingOptOut()
    {
        var user = CreateUser();
        user.UpdateUserPreferences("vi", "calm", "Asia/Ho_Chi_Minh", emailTaskNotificationsEnabled: false, CurrentTime);

        user.UpdateUserPreferences("vi", "light", "Asia/Ho_Chi_Minh", CurrentTime.AddMinutes(1));

        Assert.False(user.EmailTaskNotificationsEnabled);
        Assert.Equal("light", user.ThemePreference);
    }

    private static User CreateUser() => User.CreateUser(
        "planora.member@gmail.com",
        "PLANORA.MEMBER@GMAIL.COM",
        "Planora Member",
        CurrentTime);
}
