using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Application.Shipments.GetByOrder;

// Query = sistemi DEGISTIRMEZ. CQRS'in "read" tarafi.
// RequesterId: istegi yapan kullanici — baskasinin gonderisini goremesin diye.
public sealed record GetShipmentByOrderQuery(Guid OrderId, Guid RequesterId)
    : IRequest<Result<ShipmentDto>>;

public sealed class GetShipmentByOrderQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<GetShipmentByOrderQuery, Result<ShipmentDto>>
{
    public async Task<Result<ShipmentDto>> Handle(
        GetShipmentByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByOrderAsync(request.OrderId, cancellationToken);

        // Baskasinin gonderisi icin 403 DEGIL 404: 403, "bu siparisin
        // gonderisi var ama senin degil" bilgisini sizdirirdi.
        //
        // Sahiplik kontrolu Auth'a veya Order'a SORULMUYOR: customerId
        // event ile birlikte geldi ve burada duruyor. Servisler arasi
        // senkron cagriyi bu sekilde eliyoruz.
        if (shipment is null || shipment.CustomerId != request.RequesterId)
        {
            return Result.Failure<ShipmentDto>(ShipmentErrors.NotFound);
        }

        return ShipmentDto.From(shipment);
    }
}
