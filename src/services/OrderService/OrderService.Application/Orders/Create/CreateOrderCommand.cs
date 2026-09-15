using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Application.Orders.Create;

// CustomerId istemciden GELMEZ: controller onu JWT'nin "sub" claim'inden
// okuyup koyacak. Aksi halde bir musteri baskasinin adina siparis acabilirdi.
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string DeliveryAddress,
    PackageSize PackageSize) : IRequest<Result<Guid>>;
