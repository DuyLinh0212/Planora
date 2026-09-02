using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Sends task emails through the Planora SMTP account while presenting the acting user as
/// the author: their name appears in From, and Reply-To carries their registered address so
/// a reply reaches them directly. Sending from the user's own address would require their
/// mailbox credentials and would fail SPF/DMARC for the Planora domain.
/// </summary>
public sealed class SmtpTaskEmailNotificationSender(
    IOptions<TaskEmailNotificationOptions> options,
    ILogger<SmtpTaskEmailNotificationSender> logger) : ITaskEmailNotificationSender
{
    public async Task SendTaskNotificationAsync(TaskEmailNotification notification, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning("Task notification email was not sent because SMTP is not configured.");
            return;
        }

        var link = $"{settings.FrontendBaseUrl.TrimEnd('/')}{notification.RelativeLink}";
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, BuildFromName(notification.ActorDisplayName, settings.FromNameSuffix)),
            Subject = notification.Subject,
            Body = $"{notification.Body}{Environment.NewLine}{Environment.NewLine}{link}",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(notification.RecipientEmail, notification.RecipientDisplayName));
        message.ReplyToList.Add(new MailAddress(notification.ActorEmail, notification.ActorDisplayName));

        using var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(settings.Username))
            smtpClient.Credentials = new NetworkCredential(settings.Username, settings.Password);

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or IOException)
        {
            logger.LogError(exception, "Task notification email delivery failed.");
        }
    }

    private static string BuildFromName(string actorDisplayName, string suffix) =>
        string.IsNullOrWhiteSpace(suffix) ? actorDisplayName : $"{actorDisplayName} ({suffix})";
}
