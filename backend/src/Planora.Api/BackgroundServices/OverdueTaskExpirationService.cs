using Planora.Application.Tasks;

namespace Planora.Api.BackgroundServices;

public sealed class OverdueTaskExpirationService(IServiceScopeFactory serviceScopeFactory, ILogger<OverdueTaskExpirationService> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scanInterval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("DeadlineWorker:IntervalSeconds", 60), 10, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
                var taskService = serviceScope.ServiceProvider.GetRequiredService<TaskService>();
                var expiredTaskCount = await taskService.ExpireOverdueProjectTasksAsync(stoppingToken);
                logger.LogInformation("Deadline scan completed. ExpiredTaskCount={ExpiredTaskCount}", expiredTaskCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Deadline scan failed; the next scheduled run will retry.");
            }

            // Task.Delay observes the host shutdown token and throws when the host is stopping.
            // Keep that expected cancellation inside its own guarded block so it never escapes
            // ExecuteAsync and triggers BackgroundServiceExceptionBehavior.StopHost.
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
