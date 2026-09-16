using SmartLogistics.BuildingBlocks.Domain.Primitives;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.OrderService.Domain;

// Projenin ilk gercek entity'si. Kurallar BURADA yasar: ne controller'da,
// ne handler'da, ne de veritabani constraint'inde. Sebebi su — ayni kural
// baska bir giris yolundan (event, arka plan isi, admin paneli) da
// gecmek zorunda; entity'de olursa hicbir yol onu atlayamaz.
public sealed class Order : BaseEntity
{
    // EF Core'un nesneyi veritabanindan kurabilmesi icin.
    private Order()
    {
    }

    private Order(Guid id, Guid customerId, string deliveryAddress, PackageSize packageSize)
        : base(id)
    {
        CustomerId = customerId;
        DeliveryAddress = deliveryAddress;
        PackageSize = packageSize;
        Status = OrderStatus.Pending;
    }

    // Tum setter'lar private: bir siparis disaridan rastgele degistirilemez,
    // sadece asagidaki metotlarla (yani kurallardan gecerek) degisir.
    public Guid CustomerId { get; private set; }

    public string DeliveryAddress { get; private set; } = string.Empty;

    public PackageSize PackageSize { get; private set; }

    public OrderStatus Status { get; private set; }

    // Kurucu yerine factory: gecersiz bir Order NESNESI hic olusmasin.
    // Exception yerine Result — gecersiz adres beklenmedik bir durum degil,
    // olagan bir sonuc.
    public static Result<Order> Create(Guid customerId, string deliveryAddress, PackageSize packageSize)
    {
        if (customerId == Guid.Empty)
        {
            return Result.Failure<Order>(OrderErrors.CustomerRequired);
        }

        if (string.IsNullOrWhiteSpace(deliveryAddress))
        {
            return Result.Failure<Order>(OrderErrors.AddressRequired);
        }

        var address = deliveryAddress.Trim();

        if (address.Length > 250)
        {
            return Result.Failure<Order>(OrderErrors.AddressTooLong);
        }

        // Id kod tarafinda uretiliyor: event'in icine koyabilmek icin
        // veritabanina gitmeyi bekleyemeyiz (outbox'in on sarti).
        return new Order(Guid.NewGuid(), customerId, address, packageSize);
    }

    // Step 11: siparis yasam dongusu event'lerle surulunce cagrilacak.
    // (Step 08'de Shipment Service olustu ama Order henuz consumer degil.)
    public Result MarkAsProcessing() => TransitionTo(OrderStatus.Processing, from: OrderStatus.Pending);

    // Step 12: Delivered event'i geldiginde cagrilacak.
    public Result Complete() => TransitionTo(OrderStatus.Completed, from: OrderStatus.Processing);

    // Tamamlanmis veya zaten iptal edilmis siparis iptal edilemez.
    public Result Cancel() =>
        Status is OrderStatus.Completed or OrderStatus.Cancelled
            ? Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Cancelled))
            : Apply(OrderStatus.Cancelled);

    private Result TransitionTo(OrderStatus target, OrderStatus from) =>
        Status == from
            ? Apply(target)
            : Result.Failure(OrderErrors.InvalidTransition(Status, target));

    private Result Apply(OrderStatus target)
    {
        Status = target;
        MarkUpdated();

        return Result.Success();
    }
}
