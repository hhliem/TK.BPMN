using System.Threading.Channels;
using EnterpriseBpm.Notification.Application.Contracts;

namespace EnterpriseBpm.Notification.Infrastructure.Services;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationDispatchItem> _channel;

    public NotificationQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<NotificationDispatchItem>(options);
    }

    public ValueTask EnqueueAsync(NotificationDispatchItem item, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public ValueTask<NotificationDispatchItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}

