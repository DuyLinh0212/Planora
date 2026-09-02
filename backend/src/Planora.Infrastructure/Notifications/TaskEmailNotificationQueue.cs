using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;

namespace Planora.Infrastructure.Notifications;

/// <summary>
/// Bounded in-memory channel between the request that produced a task event and the
/// background consumer that delivers the email. Bounded on purpose: if SMTP stalls we drop
/// the oldest pending mail and log it rather than growing the queue without limit.
/// </summary>
public sealed class TaskEmailNotificationQueue : ITaskEmailNotificationQueue
{
    private readonly Channel<TaskEmailNotification> _channel;
    private readonly ILogger<TaskEmailNotificationQueue> _logger;

    public TaskEmailNotificationQueue(
        IOptions<TaskEmailNotificationOptions> options,
        ILogger<TaskEmailNotificationQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<TaskEmailNotification>(new BoundedChannelOptions(Math.Clamp(options.Value.MaxQueueLength, 10, 100_000))
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public ChannelReader<TaskEmailNotification> Reader => _channel.Reader;

    public void EnqueueTaskNotification(TaskEmailNotification notification)
    {
        if (!_channel.Writer.TryWrite(notification))
            _logger.LogWarning("Task notification email was dropped because the delivery queue is unavailable.");
    }
}
