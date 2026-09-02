using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Application.Support;

public sealed class SupportService(IPlanoraDbContext dbContext, ICurrentUser currentUser, IRealtimeNotifier realtimeNotifier, TimeProvider timeProvider)
{
    public async Task<ApplicationResult<SupportConversationResponse>> CreateSupportConversationAsync(CreateSupportConversationRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<SupportConversationResponse>(ApplicationErrors.Unauthorized());
        if (string.IsNullOrWhiteSpace(request.Subject))
            return ApplicationResult.Failure<SupportConversationResponse>(ApplicationErrors.Validation("support.subject_required", "Support subject is required.", "subject"));
        if (string.IsNullOrWhiteSpace(request.Message))
            return ApplicationResult.Failure<SupportConversationResponse>(ApplicationErrors.Validation("support.message_required", "Describe what you need help with.", "message"));
        if (request.Kind == SupportConversationKind.Refund)
        {
            var ownsPayment = request.PaymentTransactionId is Guid paymentId && await dbContext.PaymentTransactions.AnyAsync(payment => payment.Id == paymentId && payment.UserId == userId && payment.Status == Domain.Billing.PaymentStatus.Success, cancellationToken);
            if (!ownsPayment)
                return ApplicationResult.Failure<SupportConversationResponse>(ApplicationErrors.Validation("support.refund_payment_invalid", "Choose one of your successful payments for a refund request.", "paymentTransactionId"));
        }

        var now = timeProvider.GetUtcNow();
        var conversation = SupportConversation.CreateSupportConversation(userId, request.PaymentTransactionId, request.Kind, request.Subject, now);
        var message = SupportMessage.CreateSupportMessage(conversation.Id, userId, request.Message, now);
        dbContext.SupportConversations.Add(conversation);
        dbContext.SupportMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success(await MapConversationAsync(conversation, cancellationToken));
    }

    public async Task<ApplicationResult<IReadOnlyList<SupportConversationResponse>>> GetMySupportConversationsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<IReadOnlyList<SupportConversationResponse>>(ApplicationErrors.Unauthorized());
        var conversations = await dbContext.SupportConversations.Where(item => item.UserId == userId && item.DeletedAt == null).OrderByDescending(item => item.UpdatedAt).ToListAsync(cancellationToken);
        var response = new List<SupportConversationResponse>(conversations.Count);
        foreach (var conversation in conversations)
            response.Add(await MapConversationAsync(conversation, cancellationToken));
        return ApplicationResult.Success<IReadOnlyList<SupportConversationResponse>>(response);
    }

    public async Task<ApplicationResult<SupportMessageResponse>> SendSupportMessageAsync(Guid conversationId, SendSupportMessageRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<SupportMessageResponse>(ApplicationErrors.Unauthorized());
        if (string.IsNullOrWhiteSpace(request.Content))
            return ApplicationResult.Failure<SupportMessageResponse>(ApplicationErrors.Validation("support.message_required", "Message content is required.", "content"));
        var conversation = await dbContext.SupportConversations.FirstOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId && item.DeletedAt == null, cancellationToken);
        if (conversation is null)
            return ApplicationResult.Failure<SupportMessageResponse>(ApplicationErrors.NotFound("Open support conversation"));
        var now = timeProvider.GetUtcNow();
        var message = SupportMessage.CreateSupportMessage(conversation.Id, userId, request.Content, now);
        conversation.MarkWaitingForAdministrator(now);
        dbContext.SupportMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        var sender = await dbContext.Users.FindAsync([userId], cancellationToken);
        var response = new SupportMessageResponse(message.Id, userId, sender?.DisplayName ?? "User", message.Content, message.CreatedAt);
        await realtimeNotifier.NotifySupportConversationAsync(conversationId, "SupportMessageReceived", response, cancellationToken);
        return ApplicationResult.Success(response);
    }

    private async Task<SupportConversationResponse> MapConversationAsync(SupportConversation conversation, CancellationToken cancellationToken)
    {
        var messages = await (from message in dbContext.SupportMessages
                              join user in dbContext.Users on message.SenderUserId equals user.Id
                              where message.ConversationId == conversation.Id
                              orderby message.CreatedAt
                              select new SupportMessageResponse(message.Id, message.SenderUserId, user.DisplayName, message.Content, message.CreatedAt))
            .ToListAsync(cancellationToken);
        return new SupportConversationResponse(conversation.Id, conversation.Kind, conversation.Subject, conversation.Status, conversation.PaymentTransactionId, conversation.CreatedAt, conversation.ClosedAt, messages);
    }
}
