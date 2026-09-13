namespace SmartLogistics.BuildingBlocks.Application.Messaging;

// "Event'i disariya duyur" sozlesmesi. SADECE sozlesme — RabbitMQ kelimesi
// bu katmanda hic gecmez.
//
// Implementasyonu (RabbitMqEventBus) Step 06'da Infrastructure'a yazilacak.
// Buna dependency inversion denir: ust katman somut teknolojiyi degil soyut
// sozlesmeyi tanir. Yarin RabbitMQ yerine Kafka gelirse Application ve Domain
// katmanlarinda tek satir degismez.
public interface IEventBus
{
    // eventType : "OrderCreated" — routing key bundan uretilir (order.created)
    //             ve consumer mesaji hangi tipe cozecegini bundan bilir.
    // payload   : event'in JSON hali. Neden object degil string? Cunku outbox
    //             tablosunda zaten JSON olarak duruyor; nesneye cozup tekrar
    //             serialize etmek bos masraf ve veri kaybi riski.
    // messageId : outbox satirinin id'si. Consumer ayni mesaji ikinci kez
    //             gorurse bu id ile anlayip atlayacak (idempotency, Step 08).
    Task PublishAsync(
        string eventType,
        string payload,
        Guid messageId,
        CancellationToken cancellationToken = default);
}
