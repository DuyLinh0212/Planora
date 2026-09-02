using Planora.Domain.Common;

namespace Planora.Domain.Support;

public enum FeedbackStatus
{
    New,
    InReview,
    Resolved
}

public enum FeedbackPriority
{
    Low,
    Medium,
    High
}

public sealed class Feedback : Entity
{
    private Feedback() { }

    public Guid? UserId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public FeedbackStatus Status { get; private set; }
    public FeedbackPriority Priority { get; private set; }
    public Guid? AssignedAdminUserId { get; private set; }
    public string? InternalNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static Feedback CreateFeedback(
        Guid? userId,
        string category,
        string subject,
        string content,
        FeedbackPriority priority,
        DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        Category = category.Trim(),
        Subject = subject.Trim(),
        Content = content.Trim(),
        Status = FeedbackStatus.New,
        Priority = priority,
        CreatedAt = createdAt
    };

    public void AssignFeedbackToAdministrator(Guid administratorUserId)
    {
        AssignedAdminUserId = administratorUserId;
        Status = FeedbackStatus.InReview;
    }

    public void ResolveFeedback(string? internalNote, DateTimeOffset resolvedAt)
    {
        InternalNote = internalNote?.Trim();
        Status = FeedbackStatus.Resolved;
        ResolvedAt = resolvedAt;
    }
}
