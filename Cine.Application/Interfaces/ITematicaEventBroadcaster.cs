using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cine.Application.Dtos;

namespace Cine.Application.Interfaces;

public interface ITematicaEventBroadcaster
{
    (Guid SubscriptionId, ChannelReader<TematicaSitioDto> Reader) Subscribe();
    void Unsubscribe(Guid subscriptionId);
    Task PublishAsync(TematicaSitioDto tematica, CancellationToken cancellationToken = default);
}
