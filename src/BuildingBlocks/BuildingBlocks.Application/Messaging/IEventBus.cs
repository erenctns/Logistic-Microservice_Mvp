namespace SmartLogistics.BuildingBlocks.Application.Messaging;

// "Event'i disariya duyur" sozlesmesi. SADECE sozlesme — MassTransit veya
// RabbitMQ kelimesi bu katmanda hic gecmez.
//
// MassTransit'in kendi IPublishEndpoint arayuzunu dogrudan kullanabilirdik;
// kullanmiyoruz cunku o zaman Application katmani bir NuGet paketine bagimli
// olurdu. Mimari kural: interface Application'da TANIMLANIR, implementasyon
// Infrastructure'da yazilir. Bedeli 10 satirlik bir sarmalayici.
//
// PUBLISH vs SEND ayrimi:
//   Publish -> EVENT. "Siparis olustu." Kimin dinledigini gonderen bilmez,
//              dinleyen herkes bir kopya alir. Bizim kullandigimiz bu.
//   Send    -> KOMUT. "Su siparisi olustur." Tek bir kuyruga, belirli bir
//              alicaya gider. Servisler arasi komut gondermiyoruz.
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
