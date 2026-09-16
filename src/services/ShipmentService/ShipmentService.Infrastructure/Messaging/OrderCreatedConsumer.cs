using MassTransit;
using MediatR;
using SmartLogistics.Contracts.Events;
using SmartLogistics.ShipmentService.Application.Shipments.Create;

namespace SmartLogistics.ShipmentService.Infrastructure.Messaging;

// ⭐ ZINCIRIN BAGLANDIGI YER.
//
// Bu sinif bir ADAPTOR, is mantigi TASIMAZ. Controller ne ise consumer da
// odur: disaridan gelen bir tetigi Application katmanina ceviren kapi.
// Tek farki tetigin HTTP degil bir mesaj olmasi.
//
// Ayni kurali iki kapidan da gecirebilmemizin sebebi bu: siparis ister
// bir event'le ister (ileride) bir admin panelinden gelsin, ayni
// CreateShipmentCommand calisir.
//
// KUYRUK ADI: MassTransit sinif adindan uretiyor ->
// "OrderCreatedConsumer" => "order-created". Bu sinif var oldugu icin
// servis acilista o kuyrugu olusturup OrderCreated exchange'ine bagliyor.
public sealed class OrderCreatedConsumer(ISender sender) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;

        var result = await sender.Send(
            new CreateShipmentCommand(
                message.OrderId,
                message.CustomerId,
                message.DeliveryAddress,
                message.PackageSize),
            context.CancellationToken);

        if (result.IsFailure)
        {
            // Basarisizligi YUTMUYORUZ. Exception firlatinca MassTransit
            // devreye girer: 3 kez yeniden dener (1sn/2sn/4sn), yine
            // olmazsa mesaji order-created_error kuyruguna tasir.
            //
            // Sessizce loglayip gecseydik mesaj ack'lenir ve siparis
            // sonsuza kadar gonderisiz kalirdi — hem de kimse fark
            // etmeden. Gurultulu basarisizlik, sessiz veri kaybindan iyidir.
            throw new InvalidOperationException(
                $"Gonderi olusturulamadi ({message.OrderId}): {result.Error.Code} - {result.Error.Message}");
        }
    }
}
