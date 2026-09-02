using Microsoft.EntityFrameworkCore;
using Planora.Application.Common.Interfaces;
using Planora.Domain.Tasks;

namespace Planora.Application.Notifications;

/// <summary>
/// Turns a task event into one email per assignee that asked for them. Recipients who
/// disabled the option, the actor themselves, and inactive accounts are skipped.
/// </summary>
public sealed class TaskEmailNotificationService(
    IPlanoraDbContext dbContext,
    ITaskEmailNotificationQueue notificationQueue)
{
    public async Task QueueTaskEventEmailsAsync(
        ProjectTask projectTask,
        Guid actorUserId,
        IReadOnlyCollection<Guid> recipientUserIds,
        TaskEmailEvent taskEmailEvent,
        CancellationToken cancellationToken)
    {
        var targetUserIds = recipientUserIds.Where(userId => userId != actorUserId).Distinct().ToArray();
        if (targetUserIds.Length == 0)
            return;

        var actor = await dbContext.Users
            .Where(user => user.Id == actorUserId)
            .Select(user => new { user.Email, user.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);
        if (actor is null || string.IsNullOrWhiteSpace(actor.Email))
            return;

        var recipients = await dbContext.Users
            .Where(user => targetUserIds.Contains(user.Id)
                && user.EmailTaskNotificationsEnabled
                && dbContext.UserGmailLinks.Any(link => link.UserId == user.Id)
                && user.Status == Domain.Users.UserStatus.Active
                && user.Email != string.Empty)
            .Select(user => new { user.Email, user.DisplayName })
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            return;

        var projectName = await dbContext.Projects
            .Where(project => project.Id == projectTask.ProjectId)
            .Select(project => project.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Planora";
        var subject = TaskEmailTemplate.BuildSubject(taskEmailEvent, projectTask.Title);

        foreach (var recipient in recipients)
        {
            notificationQueue.EnqueueTaskNotification(new TaskEmailNotification(
                actorUserId,
                actor.DisplayName,
                actor.Email,
                recipient.Email,
                recipient.DisplayName,
                subject,
                TaskEmailTemplate.BuildBody(taskEmailEvent, projectTask, projectName, actor.DisplayName, recipient.DisplayName),
                $"/projects/{projectTask.ProjectId}/tasks"));
        }
    }
}
