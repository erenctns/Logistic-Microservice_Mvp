namespace SmartLogistics.BuildingBlocks.Domain.Primitives;

// Domain icinde OLMUS BITMIS bir olay. Isimlendirme her zaman gecmis zaman:
// OrderCreated, ShipmentDispatched. ("CreateOrder" bir komuttur, event degil.)
//
// Entity bu olayi sadece kendi listesine ekler; RabbitMQ'ya yazmaz.
// Domain katmani disariyi tanimaz — duyurmak altyapinin isidir (Step 07).
public interface IDomainEvent
{
    // Olayin kimligi. Idempotent consumer (Step 08) bunu kullanacak.
    Guid EventId { get; }

    // Olayin gerceklestigi an (UTC).
    DateTime OccurredOn { get; }
}
