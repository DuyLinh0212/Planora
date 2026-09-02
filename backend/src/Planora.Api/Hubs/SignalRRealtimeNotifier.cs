using Microsoft.AspNetCore.SignalR;
using Planora.Application.Common.Interfaces;

namespace Planora.Api.Hubs;

public sealed class SignalRRealtimeNotifier(IHubContext<PlanoraHub> hubContext) : IRealtimeNotifier
{
    public async Task NotifyUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"user_{userId}").SendAsync(eventType, payload, cancellationToken);
    }

    public async Task NotifyProjectAsync(Guid projectId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"project_{projectId}").SendAsync(eventType, payload, cancellationToken);
    }

    public async Task NotifySupportConversationAsync(Guid conversationId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"support_{conversationId}").SendAsync(eventType, payload, cancellationToken);
    }
}
