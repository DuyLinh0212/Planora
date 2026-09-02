using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Planora.Api.Hubs;

[Authorize]
public sealed class PlanoraHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinProjectGroup(string projectId)
    {
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
        }
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectId}");
        }
    }

    public async Task JoinSupportGroup(string conversationId)
    {
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"support_{conversationId}");
        }
    }

    public async Task LeaveSupportGroup(string conversationId)
    {
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"support_{conversationId}");
        }
    }
}
