using Planora.Domain.Common;

namespace Planora.Domain.Users;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsPasswordResetTokenActive(DateTimeOffset currentTime) => UsedAt is null && ExpiresAt > currentTime;

    public static PasswordResetToken CreatePasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt) => new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt
        };

    public void MarkPasswordResetTokenUsed(DateTimeOffset usedAt) => UsedAt ??= usedAt;
}
