using Planora.Domain.Common;

namespace Planora.Domain.Support;

public enum SupportConversationStatus { Open, WaitingForUser, WaitingForAdministrator, Closed }
public enum SupportConversationKind { Feedback, Refund }

public sealed class SupportConversation : AuditableEntity
{
    private SupportConversation() { }

    public Guid UserId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public SupportConversationKind Kind { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public SupportConversationStatus Status { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static SupportConversation CreateSupportConversation(
        Guid userId,
        Guid? paymentTransactionId,
        SupportConversationKind kind,
        string subject,
        DateTimeOffset createdAt)
    {
        var conversation = new SupportConversation
        {
            UserId = userId,
            PaymentTransactionId = paymentTransactionId,
            Kind = kind,
            Subject = subject.Trim(),
            Status = SupportConversationStatus.WaitingForAdministrator
        };
        conversation.MarkCreated(createdAt);
        return conversation;
    }

    public void MarkWaitingForUser(DateTimeOffset updatedAt)
    {
        Status = SupportConversationStatus.WaitingForUser;
        MarkUpdated(updatedAt);
    }

    public void MarkWaitingForAdministrator(DateTimeOffset updatedAt)
    {
        Status = SupportConversationStatus.WaitingForAdministrator;
        MarkUpdated(updatedAt);
    }

    public void CloseSupportConversation(DateTimeOffset closedAt)
    {
        Status = SupportConversationStatus.Closed;
        ClosedAt = closedAt;
        DeletedAt = closedAt;
        MarkUpdated(closedAt);
    }
}

public sealed class SupportMessage : Entity
{
    private SupportMessage() { }

    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static SupportMessage CreateSupportMessage(Guid conversationId, Guid senderUserId, string content, DateTimeOffset createdAt) => new()
    {
        ConversationId = conversationId,
        SenderUserId = senderUserId,
        Content = content.Trim(),
        CreatedAt = createdAt
    };
}
