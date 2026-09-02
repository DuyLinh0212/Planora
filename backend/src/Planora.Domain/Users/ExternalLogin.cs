using Planora.Domain.Common;

namespace Planora.Domain.Users;

public sealed class ExternalLogin : Entity
{
    private ExternalLogin() { }

    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static ExternalLogin CreateExternalLogin(Guid userId, string provider, string providerUserId, DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        Provider = provider,
        ProviderUserId = providerUserId,
        CreatedAt = createdAt
    };
}
