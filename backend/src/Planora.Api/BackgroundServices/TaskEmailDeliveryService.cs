using Planora.Application.Common.Interfaces;
using Planora.Infrastructure.Notifications;

namespace Planora.Api.BackgroundServices;

/// <summary>
/// Drains the task email queue so mail provider latency never blocks a task write. Each
/// message gets its own scope because delivery reads the sender's Gmail link from the
/// database. Delivery failures are logged; one bad email must not stop the consumer.
/// </summary>
public sealed class TaskEmailDeliveryService(
    TaskEmailNotificationQueue notificationQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TaskEmailDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var notification in notificationQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
                    var notificationSender = serviceScope.ServiceProvider.GetRequiredService<ITaskEmailNotificationSender>();
                    await notificationSender.SendTaskNotificationAsync(notification, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Task notification email could not be delivered; the queue continues.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
