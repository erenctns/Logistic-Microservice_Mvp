using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.ShipmentService.Application.Shipments.Create;

// Bu komutu bir CONTROLLER degil, bir CONSUMER tetikleyecek (8e).
// Alanlari OrderCreated event'inden geliyor.
//
// Takip numarasi burada YOK: onu servis kendisi uretir. Disaridan gelen
// veriye ne kadar az guvenirsek o kadar iyi — kimlik uretimi bizim isimiz.
public sealed record CreateShipmentCommand(
    Guid OrderId,
    Guid CustomerId,
    string DeliveryAddress,
    string PackageSize) : IRequest<Result<Guid>>;
