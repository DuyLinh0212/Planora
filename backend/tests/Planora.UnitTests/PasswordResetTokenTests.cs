using Planora.Domain.Users;

namespace Planora.UnitTests;

public sealed class PasswordResetTokenTests
{
    [Fact]
    public void NewPasswordResetToken_IsActiveBeforeExpiry()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.CreatePasswordResetToken(
            Guid.CreateVersion7(),
            "hashed-token",
            currentTime.AddMinutes(30),
            currentTime);

        Assert.True(token.IsPasswordResetTokenActive(currentTime.AddMinutes(29)));
    }

    [Fact]
    public void UsedPasswordResetToken_CannotBeUsedAgain()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var token = PasswordResetToken.CreatePasswordResetToken(
            Guid.CreateVersion7(),
            "hashed-token",
            currentTime.AddMinutes(30),
            currentTime);

        token.MarkPasswordResetTokenUsed(currentTime.AddMinutes(5));

        Assert.False(token.IsPasswordResetTokenActive(currentTime.AddMinutes(6)));
    }

    [Fact]
    public void ExpiredPasswordResetToken_IsInactive()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var expiresAt = currentTime.AddMinutes(30);
        var token = PasswordResetToken.CreatePasswordResetToken(
            Guid.CreateVersion7(),
            "hashed-token",
            expiresAt,
            currentTime);

        Assert.False(token.IsPasswordResetTokenActive(expiresAt));
    }
}
