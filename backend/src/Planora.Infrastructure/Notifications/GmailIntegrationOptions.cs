namespace Planora.Infrastructure.Notifications;

public sealed class GmailIntegrationOptions
{
    /// <summary>OAuth client used for the Gmail send consent popup. Separate from login if you prefer.</summary>
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Base64 key (32 bytes) for AES-GCM protection of stored refresh tokens. Without it the
    /// integration stays disabled rather than persisting tokens in clear text.
    /// </summary>
    public string TokenEncryptionKey { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(TokenEncryptionKey);
}
