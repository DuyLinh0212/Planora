namespace Planora.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default);
    Task NotifyProjectAsync(Guid projectId, string eventType, object payload, CancellationToken cancellationToken = default);
    Task NotifySupportConversationAsync(Guid conversationId, string eventType, object payload, CancellationToken cancellationToken = default);
}
