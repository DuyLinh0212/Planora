namespace Planora.Infrastructure.Authentication;

public sealed class PasswordResetNotificationOptions
{
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Planora";
}
