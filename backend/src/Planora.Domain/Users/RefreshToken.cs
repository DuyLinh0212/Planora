using Planora.Domain.Common;

namespace Planora.Domain.Users;

public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? DeviceInfo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsRefreshTokenActive(DateTimeOffset currentTime) => RevokedAt is null && ExpiresAt > currentTime;

    public static RefreshToken CreateRefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? deviceInfo, DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt,
        DeviceInfo = deviceInfo,
        CreatedAt = createdAt
    };

    public void RevokeRefreshToken(DateTimeOffset revokedAt) => RevokedAt ??= revokedAt;
}
