using Planora.Domain.Common;

namespace Planora.Domain.Users;

/// <summary>
/// A user's consent to let Planora send task emails through their own Gmail mailbox. The
/// refresh token is stored encrypted; only the cipher text and nonce live in the database.
/// </summary>
public sealed class UserGmailLink : AuditableEntity
{
    private UserGmailLink() { }

    public Guid UserId { get; private set; }
    public string GmailAddress { get; private set; } = string.Empty;
    public string RefreshTokenCipher { get; private set; } = string.Empty;
    public string RefreshTokenNonce { get; private set; } = string.Empty;
    public DateTimeOffset? LastSendFailedAt { get; private set; }
    public string? LastSendFailureReason { get; private set; }

    public static UserGmailLink CreateUserGmailLink(
        Guid userId,
        string gmailAddress,
        string refreshTokenCipher,
        string refreshTokenNonce,
        DateTimeOffset createdAt)
    {
        var link = new UserGmailLink
        {
            UserId = userId,
            GmailAddress = gmailAddress.Trim(),
            RefreshTokenCipher = refreshTokenCipher,
            RefreshTokenNonce = refreshTokenNonce
        };
        link.MarkCreated(createdAt);
        return link;
    }

    public void UpdateUserGmailLink(
        string gmailAddress,
        string refreshTokenCipher,
        string refreshTokenNonce,
        DateTimeOffset updatedAt)
    {
        GmailAddress = gmailAddress.Trim();
        RefreshTokenCipher = refreshTokenCipher;
        RefreshTokenNonce = refreshTokenNonce;
        LastSendFailedAt = null;
        LastSendFailureReason = null;
        MarkUpdated(updatedAt);
    }

    /// <summary>
    /// Records why Gmail refused the last send so the account page can tell the user to
    /// re-link instead of silently falling back forever.
    /// </summary>
    public void RecordGmailSendFailure(string reason, DateTimeOffset failedAt)
    {
        LastSendFailedAt = failedAt;
        LastSendFailureReason = reason.Length > 400 ? reason[..400] : reason;
        MarkUpdated(failedAt);
    }

    public void ClearGmailSendFailure(DateTimeOffset clearedAt)
    {
        if (LastSendFailedAt is null)
            return;
        LastSendFailedAt = null;
        LastSendFailureReason = null;
        MarkUpdated(clearedAt);
    }
}
