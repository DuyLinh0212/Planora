namespace Planora.Infrastructure.Notifications;

public sealed class TaskEmailNotificationOptions
{
    /// <summary>Link base used inside the email body so recipients can open the task.</summary>
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public int MaxQueueLength { get; init; } = 1000;
}
