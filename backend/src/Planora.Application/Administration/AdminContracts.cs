using Planora.Domain.Billing;
using Planora.Domain.Support;
using Planora.Domain.Users;

namespace Planora.Application.Administration;

public sealed record TimeSeriesPointResponse(DateOnly Date, decimal Value);
public sealed record CategoryMetricResponse(string Label, decimal Value);
public sealed record AdminAttentionResponse(string Code, string Label, int Count, string Severity);
public sealed record AdminActivityResponse(Guid Id, Guid? ActorUserId, string ActorDisplayName, string Action, string EntityType, string EntityId, DateTimeOffset CreatedAt);

public sealed record AdminOverviewResponse(
    int TotalUsers,
    int ActiveUsers,
    int TotalProjects,
    int ActiveProjects,
    int CompletedProjects,
    decimal SubscriptionRevenue,
    decimal PaymentSuccessRate,
    long AggregateStorageBytes,
    IReadOnlyList<TimeSeriesPointResponse> UserActivationTrend,
    IReadOnlyList<CategoryMetricResponse> ProjectStatusDistribution,
    IReadOnlyList<CategoryMetricResponse> SubscriptionDistribution,
    IReadOnlyList<TimeSeriesPointResponse> PaymentRevenueTrend,
    IReadOnlyList<AdminAttentionResponse> NeedsAttention,
    IReadOnlyList<AdminActivityResponse> RecentAdminActivity);

public sealed record AdminAccountResponse(
    Guid Id,
    string Email,
    string DisplayName,
    UserStatus Status,
    SystemRole SystemRole,
    string? PlanName,
    DateTimeOffset JoinedAt,
    DateTimeOffset LastActiveAt,
    int OwnedProjectCount,
    long StorageBytes);

public sealed record AdminAccountDetailsResponse(
    AdminAccountResponse Account,
    Guid? SubscriptionId,
    SubscriptionStatus? SubscriptionStatus,
    DateTimeOffset? SubscriptionStartedAt,
    DateTimeOffset? SubscriptionExpiresAt,
    int MaxOwnedProjects,
    long MaxStorageBytes,
    IReadOnlyList<AdminActivityResponse> RecentAdminActions);

public sealed record CreateSubscriptionPlanRequest(
    string Code,
    string Name,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int MaxOwnedProjects,
    long MaxStorageBytes,
    IReadOnlyList<string> Entitlements);

public sealed record UpdateSubscriptionPlanRequest(
    string Name,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int MaxOwnedProjects,
    long MaxStorageBytes,
    IReadOnlyList<string> Entitlements,
    bool IsActive);

public sealed record SubscriptionPlanResponse(
    Guid Id,
    string Code,
    string Name,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int MaxOwnedProjects,
    long MaxStorageBytes,
    IReadOnlyList<string> Entitlements,
    bool IsActive,
    int ActiveSubscriberCount,
    DateTimeOffset UpdatedAt);

public sealed record PaymentTransactionResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    Guid PlanId,
    string PlanName,
    PaymentProvider Provider,
    string? ProviderTransactionId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string IdempotencyKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ReviewedAt);

public sealed record FeedbackItemResponse(
    Guid Id,
    Guid? UserId,
    string UserEmail,
    string Category,
    string Subject,
    string Content,
    FeedbackStatus Status,
    FeedbackPriority Priority,
    Guid? AssignedAdminUserId,
    string? InternalNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record AssignFeedbackItemRequest(Guid AdministratorUserId);
public sealed record ResolveFeedbackItemRequest(string? InternalNote);

public sealed record AdminAnalyticsResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<TimeSeriesPointResponse> NewUsers,
    IReadOnlyList<CategoryMetricResponse> UsersByPlan,
    IReadOnlyList<CategoryMetricResponse> ProjectsByStatus,
    IReadOnlyList<CategoryMetricResponse> PaymentsByStatus,
    IReadOnlyList<TimeSeriesPointResponse> StorageGrowth);
