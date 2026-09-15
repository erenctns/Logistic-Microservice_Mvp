using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.OrderService.Application.Common.Interfaces;

namespace SmartLogistics.OrderService.Application.Orders.GetMine;

public sealed record GetMyOrdersQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<OrderDto>>>;

public sealed class GetMyOrdersQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetMyOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var results = await orders.GetByCustomerAsync(request.CustomerId, cancellationToken);

        // Bos liste bir HATA DEGILDIR: "hic siparisin yok" gecerli bir cevap.
        return Result.Success<IReadOnlyList<OrderDto>>(
            [.. results.Select(OrderDto.From)]);
    }
}
