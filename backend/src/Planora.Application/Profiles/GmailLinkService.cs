using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Users;

namespace Planora.Application.Profiles;

/// <summary>
/// Manages the current user's Gmail sending consent. Planora keeps only the encrypted refresh
/// token; the address is stored in clear text so the account page can show what is linked.
/// </summary>
public sealed class GmailLinkService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    IGmailOAuthClient gmailOAuthClient,
    ISecretProtector secretProtector,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<GmailLinkResponse>> GetMyGmailLinkAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.Unauthorized());
        var link = await dbContext.UserGmailLinks.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return ApplicationResult.Success(MapGmailLinkResponse(link));
    }

    public async Task<ApplicationResult<GmailLinkResponse>> LinkMyGmailAsync(LinkMyGmailRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.Unauthorized());
        if (!gmailOAuthClient.IsConfigured)
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.External("gmail.not_configured", "Gmail sending is not configured on the server."));
        if (string.IsNullOrWhiteSpace(request.Code))
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.Validation("gmail.code_required", "Authorization code is required.", "code"));
        if (string.IsNullOrWhiteSpace(request.RedirectUri))
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.Validation("gmail.redirect_uri_required", "Redirect URI is required.", "redirectUri"));

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.NotFound("User profile"));

        var authorization = await gmailOAuthClient.ExchangeAuthorizationCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        if (authorization.IsFailure || authorization.Value is null)
            return ApplicationResult.Failure<GmailLinkResponse>(authorization.Errors.ToArray());

        // Sending as a different mailbox than the registered one would confuse recipients and
        // break the "reply reaches the assigner" promise, so require the two to match.
        if (!string.Equals(authorization.Value.GmailAddress.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return ApplicationResult.Failure<GmailLinkResponse>(ApplicationErrors.Validation(
                "gmail.address_mismatch",
                $"Hãy liên kết đúng tài khoản {user.Email}. Bạn vừa cấp quyền cho {authorization.Value.GmailAddress}.",
                "code"));

        var protectedToken = secretProtector.Protect(authorization.Value.RefreshToken);
        var currentTime = timeProvider.GetUtcNow();
        var existingLink = await dbContext.UserGmailLinks.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (existingLink is null)
        {
            existingLink = UserGmailLink.CreateUserGmailLink(userId, authorization.Value.GmailAddress, protectedToken.Cipher, protectedToken.Nonce, currentTime);
            dbContext.UserGmailLinks.Add(existingLink);
        }
        else
        {
            existingLink.UpdateUserGmailLink(authorization.Value.GmailAddress, protectedToken.Cipher, protectedToken.Nonce, currentTime);
        }

        // Completing Gmail consent is an explicit request to receive Planora email
        // notifications. Persist that preference here so a popup success cannot leave
        // the account linked but silently opted out.
        if (!user.EmailTaskNotificationsEnabled)
            user.UpdateUserPreferences(
                user.PreferredLanguage,
                user.ThemePreference,
                user.TimeZoneId,
                emailTaskNotificationsEnabled: true,
                updatedAt: currentTime);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(MapGmailLinkResponse(existingLink));
    }

    public async Task<ApplicationResult> UnlinkMyGmailAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("User profile"));
        var link = await dbContext.UserGmailLinks.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (link is not null)
            dbContext.UserGmailLinks.Remove(link);
        // Gmail is an optional sender. Unlinking it must not opt the user out of
        // task emails because the shared SMTP mailbox remains available.
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private GmailLinkResponse MapGmailLinkResponse(UserGmailLink? link) => new(
        link is not null,
        link?.GmailAddress,
        gmailOAuthClient.IsConfigured,
        link?.LastSendFailedAt,
        link?.LastSendFailureReason);
}
