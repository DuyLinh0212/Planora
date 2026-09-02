using Planora.Domain.Support;

namespace Planora.Application.Support;

public sealed record CreateSupportConversationRequest(SupportConversationKind Kind, string Subject, string Message, Guid? PaymentTransactionId);
public sealed record SendSupportMessageRequest(string Content);
public sealed record SupportMessageResponse(Guid Id, Guid SenderUserId, string SenderDisplayName, string Content, DateTimeOffset CreatedAt);
public sealed record SupportConversationResponse(Guid Id, SupportConversationKind Kind, string Subject, SupportConversationStatus Status, Guid? PaymentTransactionId, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt, IReadOnlyList<SupportMessageResponse> Messages);
