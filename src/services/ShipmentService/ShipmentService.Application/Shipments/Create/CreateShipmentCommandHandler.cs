using MediatR;
using SmartLogistics.BuildingBlocks.Application.Messaging;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.Contracts.Events;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Application.Shipments.Create;

public sealed class CreateShipmentCommandHandler(
    IShipmentRepository shipments,
    ITrackingNumberGenerator trackingNumbers,
    IEventBus eventBus,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateShipmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        // ⭐ IDEMPOTENCY — IS KATMANI.
        //
        // Ayni siparis icin ikinci kez cagrilirsak YENI kayit acmiyoruz,
        // MEVCUT kaydin id'sini donuyoruz. Dikkat: bu bir HATA degil,
        // BASARI. Idempotent bir islemin tanimi tam olarak budur —
        // "ayni girdi, ayni sonuc, ek etki yok".
        //
        // Hata donseydik consumer mesaji islenemedi sayip retry ederdi ve
        // sonunda mesaj hata kuyruguna duserdi. Oysa yapilacak is zaten
        // yapilmis durumda.
        var existing = await shipments.GetByOrderAsync(request.OrderId, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        // Takip numarasi DISARIDAN gelmiyor, burada uretiliyor.
        var trackingNumber = await trackingNumbers.NextAsync(cancellationToken);

        // Is kurallari entity'de: gecersiz adres burada degil orada reddedilir.
        var shipment = Shipment.Create(
            request.OrderId,
            request.CustomerId,
            trackingNumber,
            request.DeliveryAddress,
            request.PackageSize);

        if (shipment.IsFailure)
        {
            return Result.Failure<Guid>(shipment.Error);
        }

        shipments.Add(shipment.Value);

        // Order Service'teki ile AYNI satir: bu cagri broker'a gitmiyor,
        // outbox_message tablosuna yazilmak uzere degisiklik takipcisine
        // giriyor. Zincirin ikinci halkasi burada doguyor.
        await eventBus.PublishAsync(
            new ShipmentCreated(
                shipment.Value.Id,
                shipment.Value.OrderId,
                shipment.Value.CustomerId,
                shipment.Value.TrackingNumber,
                shipment.Value.DeliveryAddress,
                shipment.Value.PackageSize,
                shipment.Value.CreatedAt),
            cancellationToken);

        // ATOMIK AN — ve Order Service'ten FARKLI olarak burada UC satir var:
        //   INSERT INTO inbox_state    (bu mesaji isledim damgasi)
        //   INSERT INTO shipments      (is verisi)
        //   INSERT INTO outbox_message (ShipmentCreated)
        //
        // Ucu birlikte commit edilir. Yani "gonderi olustu ama event
        // gitmedi" ya da "event gitti ama mesaj islenmedi sayildi" gibi
        // tutarsiz ara durumlar imkansiz.
        //
        // inbox_state satirini biz yazmiyoruz: consumer endpoint'ine
        // takili inbox filtresi ayni transaction'in icine ekliyor (8e).
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.Value.Id;
    }
}
