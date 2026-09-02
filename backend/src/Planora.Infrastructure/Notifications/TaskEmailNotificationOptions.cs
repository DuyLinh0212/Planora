namespace Planora.Infrastructure.Notifications;

public sealed class TaskEmailNotificationOptions
{
    /// <summary>Link base used inside the email body so recipients can open the task.</summary>
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Envelope address Planora sends from. It stays a Planora-owned mailbox so SPF/DKIM keep
    /// passing; the acting user's registered address travels in Reply-To instead.
    /// </summary>
    public string FromAddress { get; init; } = string.Empty;
    public string FromNameSuffix { get; init; } = "via Planora";
    public int MaxQueueLength { get; init; } = 1000;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(FromAddress);
}
