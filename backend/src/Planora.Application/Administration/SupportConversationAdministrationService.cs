using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Application.Support;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Application.Administration;

public sealed class SupportConversationAdministrationService(
    IPlanoraDbContext dbContext,
    ICurrentUser currentUser,
    AdminAuthorizationService authorizationService,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider)
{
    public async Task<ApplicationResult<IReadOnlyList<SupportConversationResponse>>> GetSupportConversationsAsync(SupportConversationStatus? status, CancellationToken cancellationToken)
    {
        var authorizationError = await authorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<IReadOnlyList<SupportConversationResponse>>(authorizationError);
        var query = dbContext.SupportConversations.AsQueryable();
        if (status is not null)
            query = query.Where(conversation => conversation.Status == status);
        var conversations = await query.OrderByDescending(conversation => conversation.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        var response = new List<SupportConversationResponse>(conversations.Count);
        foreach (var conversation in conversations)
            response.Add(await MapConversationAsync(conversation, cancellationToken));
        return ApplicationResult.Success<IReadOnlyList<SupportConversationResponse>>(response);
    }

    public async Task<ApplicationResult<SupportMessageResponse>> SendAdministratorSupportMessageAsync(Guid conversationId, SendSupportMessageRequest request, CancellationToken cancellationToken)
    {
        var authorizationError = await authorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure<SupportMessageResponse>(authorizationError);
        if (string.IsNullOrWhiteSpace(request.Content))
            return ApplicationResult.Failure<SupportMessageResponse>(ApplicationErrors.Validation("support.message_required", "Message content is required.", "content"));
        var conversation = await dbContext.SupportConversations.FirstOrDefaultAsync(item => item.Id == conversationId && item.DeletedAt == null, cancellationToken);
        if (conversation is null)
            return ApplicationResult.Failure<SupportMessageResponse>(ApplicationErrors.NotFound("Open support conversation"));
        var now = timeProvider.GetUtcNow();
        var message = SupportMessage.CreateSupportMessage(conversationId, currentUser.UserId!.Value, request.Content, now);
        conversation.MarkWaitingForUser(now);
        dbContext.SupportMessages.Add(message);
        dbContext.UserNotifications.Add(UserNotification.CreateUserNotification(conversation.UserId, "support.reply", "Hỗ trợ Planora đã phản hồi", request.Content, nameof(SupportConversation), conversationId.ToString(), now));
        await dbContext.SaveChangesAsync(cancellationToken);
        var sender = await dbContext.Users.FindAsync([currentUser.UserId.Value], cancellationToken);
        var response = new SupportMessageResponse(message.Id, message.SenderUserId, sender?.DisplayName ?? "Planora support", message.Content, message.CreatedAt);
        await realtimeNotifier.NotifySupportConversationAsync(conversationId, "SupportMessageReceived", response, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(conversation.UserId, "NotificationReceived", new { type = "support.reply", title = "Hỗ trợ Planora đã phản hồi", message = request.Content, conversationId }, cancellationToken);
        return ApplicationResult.Success(response);
    }

    public async Task<ApplicationResult> CloseSupportConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var authorizationError = await authorizationService.GetSystemAdministratorAuthorizationErrorAsync(cancellationToken);
        if (authorizationError is not null)
            return ApplicationResult.Failure(authorizationError);
        var conversation = await dbContext.SupportConversations.FirstOrDefaultAsync(item => item.Id == conversationId && item.DeletedAt == null, cancellationToken);
        if (conversation is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Open support conversation"));
        conversation.CloseSupportConversation(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<SupportConversationResponse> MapConversationAsync(SupportConversation conversation, CancellationToken cancellationToken)
    {
        var messages = await (from message in dbContext.SupportMessages
                              join user in dbContext.Users on message.SenderUserId equals user.Id
                              where message.ConversationId == conversation.Id
                              orderby message.CreatedAt
                              select new SupportMessageResponse(message.Id, message.SenderUserId, user.DisplayName, message.Content, message.CreatedAt)).ToListAsync(cancellationToken);
        return new SupportConversationResponse(conversation.Id, conversation.Kind, conversation.Subject, conversation.Status, conversation.PaymentTransactionId, conversation.CreatedAt, conversation.ClosedAt, messages);
    }
}
