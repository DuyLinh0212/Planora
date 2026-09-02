using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Interfaces;
using Planora.Infrastructure.Persistence;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Chooses how a task email leaves Planora. If the acting user linked their Gmail account the
/// mail is sent as them through the Gmail API, so the recipient sees their real address. Users
/// who have not linked fall back to the shared Planora mailbox with Reply-To pointing at them,
/// which keeps notifications working instead of silently dropping them.
/// </summary>
public sealed class TaskEmailNotificationDispatcher(
    PlanoraDbContext dbContext,
    IGmailOAuthClient gmailOAuthClient,
    IGmailMessageSender gmailMessageSender,
    ISecretProtector secretProtector,
    SmtpTaskEmailNotificationSender smtpSender,
    TimeProvider timeProvider,
    ILogger<TaskEmailNotificationDispatcher> logger) : ITaskEmailNotificationSender
{
    public async Task SendTaskNotificationAsync(TaskEmailNotification notification, CancellationToken cancellationToken)
    {
        if (await TrySendThroughLinkedGmailAsync(notification, cancellationToken))
            return;
        await smtpSender.SendTaskNotificationAsync(notification, cancellationToken);
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
            logger.LogError(exception, "Stored Gmail authorization could not be read; falling back to the Planora mailbox.");
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
            logger.LogWarning("Gmail send failed for user {UserId}; falling back to the Planora mailbox.", notification.ActorUserId);
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
