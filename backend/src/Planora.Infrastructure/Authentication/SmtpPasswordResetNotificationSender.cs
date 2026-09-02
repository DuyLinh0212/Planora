using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;

namespace Planora.Infrastructure.Authentication;

public sealed class SmtpPasswordResetNotificationSender(
    IOptions<PasswordResetNotificationOptions> options,
    ILogger<SmtpPasswordResetNotificationSender> logger) : IPasswordResetNotificationSender
{
    public async Task SendPasswordResetInstructionsAsync(
        string email,
        string displayName,
        string resetToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.FromAddress))
        {
            logger.LogWarning("Password reset email was not sent because SMTP is not configured.");
            return;
        }

        var resetUrl = $"{settings.FrontendBaseUrl.TrimEnd('/')}/reset-password?code={Uri.EscapeDataString(resetToken)}";
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "Reset your Planora password",
            Body = $"Hello {displayName},\n\nYour Planora password reset code is {resetToken}.\n\nYou can also open:\n{resetUrl}\n\nThe code expires at {expiresAt:O} and can be used once.",
            IsBodyHtml = false
        };
        message.To.Add(email);

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
        catch (Exception exception)
        {
            logger.LogError(exception, "Password reset email delivery failed.");
        }
    }
}
