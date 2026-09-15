using MassTransit;
using SmartLogistics.BuildingBlocks.Application.Messaging;

namespace SmartLogistics.BuildingBlocks.Infrastructure.Messaging;

// IEventBus'in MassTransit ile gerceklestirimi: tek isi cagriyi
// IPublishEndpoint'e devretmek.
//
// Step 07'de outbox devreye girdiginde bu sinifin kodu DEGISMEYECEK:
// MassTransit, DbContext'e bagli bir scope icinde IPublishEndpoint'i
// outbox'a yazan bir implementasyonla degistiriyor. Yani "once veritabanina
// yaz, sonra yayinla" davranisi konfigurasyonla geliyor, kodla degil.
public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(@event, cancellationToken);
}
