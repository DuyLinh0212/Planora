using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;
using Planora.Domain.Projects;
using Planora.Domain.Users;

namespace Planora.Application.Notifications;

public sealed class NotificationService(IPlanoraDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private const int RetentionDays = 7;

    public async Task<ApplicationResult<IReadOnlyList<UserNotificationResponse>>> GetMyNotificationsAsync(bool unreadOnly, bool includeDismissed, int? limit, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure<IReadOnlyList<UserNotificationResponse>>(ApplicationErrors.Unauthorized());
        var cutoff = timeProvider.GetUtcNow().AddDays(-RetentionDays);
        var query = dbContext.UserNotifications.Where(notification =>
            notification.UserId == userId &&
            notification.DeletedAt == null &&
            notification.CreatedAt >= cutoff);
        if (unreadOnly)
            query = query.Where(notification => notification.ReadAt == null);
        if (!includeDismissed)
            query = query.Where(notification => notification.DismissedAt == null);
        IQueryable<UserNotification> orderedQuery = query.OrderByDescending(notification => notification.CreatedAt);
        if (limit is int requestedLimit)
            orderedQuery = orderedQuery.Take(Math.Clamp(requestedLimit, 1, 100));
        var notifications = await orderedQuery
            .Select(notification => new UserNotificationResponse(notification.Id, notification.Type, notification.Title, notification.Message, notification.EntityType, notification.EntityId, notification.CreatedAt, notification.ReadAt, notification.DismissedAt))
            .ToListAsync(cancellationToken);
        var invitationIds = notifications
            .Where(notification => notification.Type == "project.invitation" && notification.EntityId is not null)
            .Select(notification => Guid.TryParse(notification.EntityId, out var invitationId) ? invitationId : Guid.Empty)
            .Where(invitationId => invitationId != Guid.Empty)
            .ToArray();
        var actionableInvitationIds = invitationIds.Length == 0
            ? []
            : await dbContext.ProjectInvitations
                .Where(invitation =>
                    invitationIds.Contains(invitation.Id) &&
                    invitation.Status == InvitationStatus.Pending &&
                    invitation.ExpiresAt > timeProvider.GetUtcNow())
                .Select(invitation => invitation.Id)
                .ToListAsync(cancellationToken);
        var actionableIdSet = actionableInvitationIds.ToHashSet();
        var responses = notifications
            .Select(notification => notification with
            {
                IsActionable = Guid.TryParse(notification.EntityId, out var invitationId) && actionableIdSet.Contains(invitationId)
            })
            .ToList();
        return ApplicationResult.Success<IReadOnlyList<UserNotificationResponse>>(responses);
    }

    public async Task<ApplicationResult> MarkMyNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId && item.DeletedAt == null, cancellationToken);
        if (notification is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Notification"));
        notification.MarkUserNotificationRead(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> DismissMyNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return ApplicationResult.Failure(ApplicationErrors.Unauthorized());
        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(
            item => item.Id == notificationId && item.UserId == userId && item.DeletedAt == null,
            cancellationToken);
        if (notification is null)
            return ApplicationResult.Failure(ApplicationErrors.NotFound("Notification"));
        notification.Dismiss(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public Task<int> SoftDeleteExpiredNotificationsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now.AddDays(-RetentionDays);
        return dbContext.UserNotifications
            .Where(notification => notification.DeletedAt == null && notification.CreatedAt < cutoff)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.DeletedAt, now),
                cancellationToken);
    }
}
