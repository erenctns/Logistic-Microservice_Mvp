using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.OrderService.Application.Common.Interfaces;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Application.Orders.GetById;

// Query = sistemi DEGISTIRMEZ. CQRS'in "read" tarafi.
// RequesterId: istegi yapan kullanici — baskasinin siparisini goremesin diye.
public sealed record GetOrderByIdQuery(Guid OrderId, Guid RequesterId) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);

        // Baskasinin siparisi icin 403 DEGIL 404 donuyoruz: 403,
        // "bu id'de bir siparis var ama senin degil" bilgisini sizdirirdi.
        // Auth'ta "kullanici yok" ile "sifre yanlis" ayrimini yapmamamizla ayni mantik.
        if (order is null || order.CustomerId != request.RequesterId)
        {
            return Result.Failure<OrderDto>(OrderErrors.NotFound);
        }

        return OrderDto.From(order);
    }
}
