using Planora.Application.Notifications;

namespace Planora.Api.BackgroundServices;

public sealed class NotificationRetentionService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationRetentionService> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scanInterval = TimeSpan.FromMinutes(Math.Clamp(
            configuration.GetValue("NotificationRetention:IntervalMinutes", 60),
            5,
            1440));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var deletedCount = await notificationService.SoftDeleteExpiredNotificationsAsync(stoppingToken);
                if (deletedCount > 0)
                    logger.LogInformation("Expired notifications soft-deleted. Count={DeletedCount}", deletedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification retention scan failed; the next run will retry.");
            }

            try
            {
                await Task.Delay(scanInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
