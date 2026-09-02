using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Interfaces;
using Planora.Infrastructure.Persistence;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Sends a task email only when the acting user has explicitly linked their Gmail account.
/// Without that consent, the in-app notification remains the delivery channel. The recipient's
/// Gmail link is not required because the message is sent to their registered email address.
/// </summary>
public sealed class TaskEmailNotificationDispatcher(
    PlanoraDbContext dbContext,
    IGmailOAuthClient gmailOAuthClient,
    IGmailMessageSender gmailMessageSender,
    ISecretProtector secretProtector,
    TimeProvider timeProvider,
    ILogger<TaskEmailNotificationDispatcher> logger) : ITaskEmailNotificationSender
{
    public async Task SendTaskNotificationAsync(TaskEmailNotification notification, CancellationToken cancellationToken)
    {
        if (!await TrySendThroughLinkedGmailAsync(notification, cancellationToken))
            logger.LogInformation(
                "Task email was not sent because the acting user has no usable linked Gmail account. The in-app notification remains available.");
    }

    private async Task<bool> TrySendThroughLinkedGmailAsync(TaskEmailNotification notification, CancellationToken cancellationToken)
    {
        if (!gmailOAuthClient.IsConfigured)
            return false;

        var gmailLink = await dbContext.UserGmailLinks.FirstOrDefaultAsync(link => link.UserId == notification.ActorUserId, cancellationToken);
        if (gmailLink is null)
            return false;

        string refreshToken;
        try
        {
            refreshToken = secretProtector.Unprotect(gmailLink.RefreshTokenCipher, gmailLink.RefreshTokenNonce);
        }
        catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException or InvalidOperationException)
        {
            logger.LogError(exception, "Stored Gmail authorization could not be read; task email was not sent.");
            await RecordFailureAsync(gmailLink, "Không đọc được liên kết Gmail đã lưu. Hãy liên kết lại.", cancellationToken);
            return false;
        }

        var accessToken = await gmailOAuthClient.CreateAccessTokenAsync(refreshToken, cancellationToken);
        if (accessToken.IsFailure || string.IsNullOrWhiteSpace(accessToken.Value))
        {
            await RecordFailureAsync(gmailLink, accessToken.Errors.FirstOrDefault()?.Message ?? "Google từ chối liên kết Gmail.", cancellationToken);
            return false;
        }

        var sendResult = await gmailMessageSender.SendGmailMessageAsync(accessToken.Value, notification, cancellationToken);
        if (sendResult.IsFailure)
        {
            logger.LogWarning("Gmail send failed for user {UserId}; task email was not sent.", notification.ActorUserId);
            await RecordFailureAsync(gmailLink, sendResult.Errors.FirstOrDefault()?.Message ?? "Gmail từ chối gửi thư.", cancellationToken);
            return false;
        }

        gmailLink.ClearGmailSendFailure(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task RecordFailureAsync(Domain.Users.UserGmailLink gmailLink, string reason, CancellationToken cancellationToken)
    {
        gmailLink.RecordGmailSendFailure(reason, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
