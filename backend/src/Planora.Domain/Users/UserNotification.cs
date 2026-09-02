using Planora.Domain.Common;

namespace Planora.Domain.Users;

public sealed class UserNotification : Entity
{
    private UserNotification() { }

    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? DismissedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static UserNotification CreateUserNotification(
        Guid userId,
        string type,
        string title,
        string message,
        string? entityType,
        string? entityId,
        DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        Type = type.Trim(),
        Title = title.Trim(),
        Message = message.Trim(),
        EntityType = entityType?.Trim(),
        EntityId = entityId?.Trim(),
        CreatedAt = createdAt
    };

    public void MarkUserNotificationRead(DateTimeOffset readAt) => ReadAt ??= readAt;

    public void Dismiss(DateTimeOffset dismissedAt) => DismissedAt ??= dismissedAt;

    public void SoftDelete(DateTimeOffset deletedAt) => DeletedAt ??= deletedAt;
}
