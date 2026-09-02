namespace Planora.Application.Notifications;

public sealed record UserNotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? EntityType,
    string? EntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? DismissedAt,
    bool IsActionable = false);
