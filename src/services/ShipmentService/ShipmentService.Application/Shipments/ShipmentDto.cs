using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Application.Shipments;

// Disariya donen sekil. Entity'yi dogrudan dondurmuyoruz:
// entity degisirse API sozlesmesi sessizce degisir, istemciler kirilir.
public sealed record ShipmentDto(
    Guid Id,
    Guid OrderId,
    string TrackingNumber,
    string DeliveryAddress,
    string PackageSize,
    string Status,
    DateTime CreatedAt)
{
    public static ShipmentDto From(Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.TrackingNumber,
        shipment.DeliveryAddress,
        shipment.PackageSize,
        shipment.Status.ToString(),
        shipment.CreatedAt);
}
