using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.ShipmentService.Domain;

// Is kurali ihlalleri, Order Service'teki kalibin ayni: kod makine icin,
// mesaj insan icin.
public static class ShipmentErrors
{
    public static readonly Error OrderRequired =
        new("shipment.order_required", "Gonderi bir siparise ait olmalidir.");

    public static readonly Error CustomerRequired =
        new("shipment.customer_required", "Gonderi bir musteriye ait olmalidir.");

    public static readonly Error TrackingNumberRequired =
        new("shipment.tracking_number_required", "Takip numarasi bos olamaz.");

    public static readonly Error AddressRequired =
        new("shipment.address_required", "Teslimat adresi bos olamaz.");

    public static readonly Error AddressTooLong =
        new("shipment.address_too_long", "Teslimat adresi en fazla 250 karakter olabilir.");

    public static readonly Error PackageSizeRequired =
        new("shipment.package_size_required", "Paket boyutu bos olamaz.");

    public static readonly Error NotFound =
        new("shipment.not_found", "Gonderi bulunamadi.");

    public static Error InvalidTransition(ShipmentStatus from, ShipmentStatus to) =>
        new("shipment.invalid_transition", $"Gonderi {from} durumundan {to} durumuna gecemez.");
}
