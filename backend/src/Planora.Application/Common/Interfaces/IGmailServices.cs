using Planora.Application.Common.Results;

namespace Planora.Application.Common.Interfaces;

/// <summary>Protects Gmail refresh tokens at rest. Cipher and nonce are stored separately.</summary>
public interface ISecretProtector
{
    (string Cipher, string Nonce) Protect(string plainText);
    string Unprotect(string cipher, string nonce);
}

public sealed record GmailAuthorization(string GmailAddress, string RefreshToken);

/// <summary>
/// Exchanges the popup's authorization code for a long-lived refresh token, and later trades
/// that refresh token for the short-lived access token used to call the Gmail API.
/// </summary>
public interface IGmailOAuthClient
{
    bool IsConfigured { get; }
    Task<ApplicationResult<GmailAuthorization>> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<ApplicationResult<string>> CreateAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
}

/// <summary>
/// Sends one already-composed message through the acting user's own Gmail mailbox.
/// </summary>
public interface IGmailMessageSender
{
    Task<ApplicationResult> SendGmailMessageAsync(string accessToken, TaskEmailNotification notification, CancellationToken cancellationToken);
}
