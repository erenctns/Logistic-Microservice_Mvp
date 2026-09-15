using MediatR;
using SmartLogistics.BuildingBlocks.Application.Messaging;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.Contracts.Events;
using SmartLogistics.OrderService.Application.Common.Interfaces;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Application.Orders.Create;

public sealed class CreateOrderCommandHandler(
    IOrderRepository orders,
    IEventBus eventBus,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Is kurallari entity'de: gecersiz adres burada degil orada reddedilir.
        var order = Order.Create(request.CustomerId, request.DeliveryAddress, request.PackageSize);

        if (order.IsFailure)
        {
            return Result.Failure<Guid>(order.Error);
        }

        orders.Add(order.Value);

        // DIKKAT: bu satir mesaji RabbitMQ'ya GONDERMIYOR.
        // Outbox acik oldugu icin (AddEntityFrameworkOutbox + UseBusOutbox)
        // MassTransit event'i outbox_message TABLOSUNA yazmak uzere
        // DbContext'in degisiklik takipcisine ekliyor.
        await eventBus.PublishAsync(
            new OrderCreated(
                order.Value.Id,
                order.Value.CustomerId,
                order.Value.DeliveryAddress,
                order.Value.PackageSize.ToString(),
                order.Value.CreatedAt),
            cancellationToken);

        // ATOMIK AN. Bu tek cagri iki INSERT'i ayni transaction'da yapiyor:
        //   INSERT INTO orders         (...)
        //   INSERT INTO outbox_message (OrderCreated)
        // Ikisi ya birlikte olur ya hic olmaz — dual write problemi burada cozuluyor.
        //
        // Commit'ten SONRA MassTransit'in arka plan servisi outbox_message
        // tablosunu tarayip mesaji broker'a basiyor ve satiri temizliyor.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Value.Id;
    }
}
