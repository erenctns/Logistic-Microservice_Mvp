using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Application.Orders;

// Disariya donen sekil. Entity'yi dogrudan dondurmuyoruz:
// entity degisirse API sozlesmesi sessizce degisir, istemciler kirilir.
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string DeliveryAddress,
    string PackageSize,
    string Status,
    DateTime CreatedAt)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.CustomerId,
        order.DeliveryAddress,
        order.PackageSize.ToString(),
        order.Status.ToString(),
        order.CreatedAt);
}
