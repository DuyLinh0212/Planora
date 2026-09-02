using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Sends through the Gmail API as the acting user, so the recipient sees the message coming
/// from that user's own registered address with Gmail's own SPF/DKIM signature.
/// </summary>
public sealed class GmailMessageSender(HttpClient httpClient, IOptions<TaskEmailNotificationOptions> options) : IGmailMessageSender
{
    private const string SendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    public async Task<ApplicationResult> SendGmailMessageAsync(
        string accessToken,
        TaskEmailNotification notification,
        CancellationToken cancellationToken)
    {
        var rawMessage = BuildRawMessage(notification, options.Value.FrontendBaseUrl);
        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint)
        {
            Content = JsonContent.Create(new { raw = rawMessage })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return ApplicationResult.Success();

            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            return ApplicationResult.Failure(ApplicationErrors.External(
                "gmail.send_rejected",
                $"Gmail returned {(int)response.StatusCode}: {Truncate(detail, 300)}"));
        }
        catch (HttpRequestException)
        {
            return ApplicationResult.Failure(ApplicationErrors.External(
                "gmail.send_unavailable",
                "Could not reach Gmail. The notification will use the fallback mailbox."));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApplicationResult.Failure(ApplicationErrors.External(
                "gmail.send_timeout",
                "Gmail took too long to respond. The notification will use the fallback mailbox."));
        }
    }

    /// <summary>
    /// Builds an RFC 2822 message and encodes it base64url as the Gmail API expects. Headers
    /// carry Vietnamese text, so they use RFC 2047 encoded-words rather than raw UTF-8.
    /// </summary>
    internal static string BuildRawMessage(TaskEmailNotification notification, string frontendBaseUrl)
    {
        var link = $"{frontendBaseUrl.TrimEnd('/')}{notification.RelativeLink}";
        var body = $"{notification.Body}{Environment.NewLine}{Environment.NewLine}{link}";
        var message = new StringBuilder()
            .Append("From: ").Append(EncodeAddress(notification.ActorDisplayName, notification.ActorEmail)).Append("\r\n")
            .Append("To: ").Append(EncodeAddress(notification.RecipientDisplayName, notification.RecipientEmail)).Append("\r\n")
            .Append("Subject: ").Append(EncodeHeaderValue(notification.Subject)).Append("\r\n")
            .Append("MIME-Version: 1.0\r\n")
            .Append("Content-Type: text/plain; charset=utf-8\r\n")
            .Append("Content-Transfer-Encoding: base64\r\n\r\n")
            .Append(WrapBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(body))))
            .ToString();

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(message))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string EncodeAddress(string displayName, string email) =>
        string.IsNullOrWhiteSpace(displayName) ? email : $"{EncodeHeaderValue(displayName)} <{email}>";

    private static string EncodeHeaderValue(string value) =>
        value.All(character => character < 128)
            ? value
            : $"=?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";

    private static string WrapBase64(string base64)
    {
        var builder = new StringBuilder(base64.Length + base64.Length / 76 * 2);
        for (var offset = 0; offset < base64.Length; offset += 76)
            builder.Append(base64, offset, Math.Min(76, base64.Length - offset)).Append("\r\n");
        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
