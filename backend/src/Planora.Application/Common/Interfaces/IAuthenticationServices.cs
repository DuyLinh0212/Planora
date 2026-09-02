using Planora.Application.Common.Results;
using Planora.Domain.Users;

namespace Planora.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string passwordHash, string password);
}

public sealed record OpaqueToken(string Value, string Hash);
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
public sealed record ExternalIdentity(string ProviderUserId, string Email, string DisplayName, string? AvatarUrl);

public interface ITokenIssuer
{
    AccessToken CreateAccessToken(User user, DateTimeOffset issuedAt);
    OpaqueToken CreateOpaqueToken();
    string HashOpaqueToken(string token);
}

public interface IExternalIdentityVerifier
{
    Task<ApplicationResult<ExternalIdentity>> VerifyExternalIdentityAsync(string provider, string token, CancellationToken cancellationToken);
}

public interface IPasswordResetNotificationSender
{
    Task SendPasswordResetInstructionsAsync(
        string email,
        string displayName,
        string resetToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
