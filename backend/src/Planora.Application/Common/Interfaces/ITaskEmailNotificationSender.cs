namespace Planora.Application.Common.Interfaces;

/// <summary>
/// One task email addressed to a single recipient. The acting user is carried along so
/// delivery can send through their own linked Gmail mailbox when they have one.
/// </summary>
public sealed record TaskEmailNotification(
    Guid ActorUserId,
    string ActorDisplayName,
    string ActorEmail,
    string RecipientEmail,
    string RecipientDisplayName,
    string Subject,
    string Body,
    string RelativeLink);

/// <summary>
/// Accepts task emails without blocking the request that produced them. Delivery happens
/// on a background consumer so a slow or unavailable mail provider never fails a task write.
/// </summary>
public interface ITaskEmailNotificationQueue
{
    void EnqueueTaskNotification(TaskEmailNotification notification);
}

public interface ITaskEmailNotificationSender
{
    Task SendTaskNotificationAsync(TaskEmailNotification notification, CancellationToken cancellationToken);
}
