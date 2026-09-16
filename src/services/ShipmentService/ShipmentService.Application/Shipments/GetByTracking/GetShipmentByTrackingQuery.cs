using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Application.Shipments.GetByTracking;

// Servisin vitrin sorgusu: musteri takip numarasiyla gonderisini arar.
public sealed record GetShipmentByTrackingQuery(string TrackingNumber, Guid RequesterId)
    : IRequest<Result<ShipmentDto>>;

public sealed class GetShipmentByTrackingQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<GetShipmentByTrackingQuery, Result<ShipmentDto>>
{
    public async Task<Result<ShipmentDto>> Handle(
        GetShipmentByTrackingQuery request,
        CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByTrackingNumberAsync(request.TrackingNumber, cancellationToken);

        // Takip numarasi tahmin edilebilir bir seri (TR-2026-00001, 00002...).
        // Sahiplik kontrolu olmasaydi biri sirayla deneyerek baskalarinin
        // adreslerini toplayabilirdi. Bulunabilir olmak yetkili olmak degildir.
        if (shipment is null || shipment.CustomerId != request.RequesterId)
        {
            return Result.Failure<ShipmentDto>(ShipmentErrors.NotFound);
        }

        return ShipmentDto.From(shipment);
    }
}
