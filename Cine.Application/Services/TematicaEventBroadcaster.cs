using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;

namespace Cine.Application.Services;

public class TematicaEventBroadcaster : ITematicaEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<TematicaSitioDto>> _subscribers = new();

    public (Guid SubscriptionId, ChannelReader<TematicaSitioDto> Reader) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<TematicaSitioDto>(new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _subscribers.TryAdd(id, channel);
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public Task PublishAsync(TematicaSitioDto tematica, CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(tematica);
        }

        return Task.CompletedTask;
    }
}
