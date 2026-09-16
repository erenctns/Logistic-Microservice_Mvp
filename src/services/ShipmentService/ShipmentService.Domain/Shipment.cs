using SmartLogistics.BuildingBlocks.Domain.Primitives;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.ShipmentService.Domain;

// Bir siparisin fiziksel karsiligi. Order Service "ne istendi"yi tutar,
// burasi "nasil tasiniyor"u.
//
// ONEMLI: bu entity HTTP ile degil, SADECE OrderCreated event'i ile dogar.
// Disaridan shipment yaratan bir endpoint bilerek yok — gonderi siparisin
// sonucudur, bagimsiz bir varlik degil.
public sealed class Shipment : BaseEntity
{
    // EF Core'un nesneyi veritabanindan kurabilmesi icin.
    private Shipment()
    {
    }

    private Shipment(
        Guid id,
        Guid orderId,
        Guid customerId,
        string trackingNumber,
        string deliveryAddress,
        string packageSize)
        : base(id)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TrackingNumber = trackingNumber;
        DeliveryAddress = deliveryAddress;
        PackageSize = packageSize;
        Status = ShipmentStatus.Created;
    }

    // Hangi siparisten dogdu. Veritabaninda UNIQUE (8c): bir siparisin
    // bir gonderisi olur — idempotency'nin is katmanindaki ayagi.
    public Guid OrderId { get; private set; }

    // Event'ten tasiniyor. Sahiplik kontrolu icin gerekli: "bu gonderiyi
    // kim gorebilir" sorusunu Auth'a istek atmadan cevaplayabilelim diye.
    public Guid CustomerId { get; private set; }

    public string TrackingNumber { get; private set; } = string.Empty;

    public string DeliveryAddress { get; private set; } = string.Empty;

    // Neden enum degil string? Cunku bu servis paket boyutuna gore HICBIR
    // karar vermiyor; degeri aldigi gibi yayinladigi event'e tasiyor.
    // Enum'u burada da tanimlarsak ayni kavramin ikinci
    // bir dogruluk kaynagi olur ve Order'a her yeni boyut eklendiginde
    // burayi da guncellemek gerekir. Anlamlandirmadigimiz veriyi oldugu
    // gibi tasimak daha durust.
    public string PackageSize { get; private set; } = string.Empty;

    public ShipmentStatus Status { get; private set; }

    public static Result<Shipment> Create(
        Guid orderId,
        Guid customerId,
        string trackingNumber,
        string deliveryAddress,
        string packageSize)
    {
        if (orderId == Guid.Empty)
        {
            return Result.Failure<Shipment>(ShipmentErrors.OrderRequired);
        }

        if (customerId == Guid.Empty)
        {
            return Result.Failure<Shipment>(ShipmentErrors.CustomerRequired);
        }

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            return Result.Failure<Shipment>(ShipmentErrors.TrackingNumberRequired);
        }

        if (string.IsNullOrWhiteSpace(deliveryAddress))
        {
            return Result.Failure<Shipment>(ShipmentErrors.AddressRequired);
        }

        var address = deliveryAddress.Trim();

        if (address.Length > 250)
        {
            return Result.Failure<Shipment>(ShipmentErrors.AddressTooLong);
        }

        if (string.IsNullOrWhiteSpace(packageSize))
        {
            return Result.Failure<Shipment>(ShipmentErrors.PackageSizeRequired);
        }

        // Takip numarasi BUYUK HARFE cevriliyor. Sebebi arama: musteri
        // "tr-2026-00001" yazdiginda da bulabilmeli. Normalizasyonu yazma
        // aninda bir kez yapmak, her sorguda LOWER() cagirmaktan iyidir
        // (o index'i de kullanilamaz hale getirirdi).
        return new Shipment(
            Guid.NewGuid(),
            orderId,
            customerId,
            trackingNumber.Trim().ToUpperInvariant(),
            address,
            packageSize.Trim());
    }

    // Asagidaki gecisleri su an tetikleyen bir giris yolu yok: gonderi
    // olusturulup Created'da kaliyor. Kural yine de entity'de eksiksiz
    // duruyor — bir teslimat akisi eklenirse cagiracagi yer burasi.
    public Result MarkAssigned() => TransitionTo(ShipmentStatus.Assigned, from: ShipmentStatus.Created);

    public Result MarkInTransit() => TransitionTo(ShipmentStatus.InTransit, from: ShipmentStatus.Assigned);

    public Result MarkDelivered() => TransitionTo(ShipmentStatus.Delivered, from: ShipmentStatus.InTransit);

    private Result TransitionTo(ShipmentStatus target, ShipmentStatus from) =>
        Status == from
            ? Apply(target)
            : Result.Failure(ShipmentErrors.InvalidTransition(Status, target));

    private Result Apply(ShipmentStatus target)
    {
        Status = target;
        MarkUpdated();

        return Result.Success();
    }
}
