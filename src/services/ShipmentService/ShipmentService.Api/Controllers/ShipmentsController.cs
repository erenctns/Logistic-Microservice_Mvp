using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;
using SmartLogistics.ShipmentService.Api.Common;
using SmartLogistics.ShipmentService.Application.Shipments.GetByOrder;
using SmartLogistics.ShipmentService.Application.Shipments.GetByTracking;

namespace SmartLogistics.ShipmentService.Api.Controllers;

// SADECE OKUMA. Bilerek POST/PUT/DELETE yok: gonderi bir siparisin
// sonucudur, bagimsiz olarak yaratilamaz. Yazma yolunun tek kapisi
// OrderCreatedConsumer.
//
// Bu, "her servise CRUD yaz" refleksine karsi bilincli bir durus:
// endpoint listesi veri modelini degil IS AKISINI yansitmali.
[ApiController]
[Route("api/shipments")]
[Authorize]
public sealed class ShipmentsController(ISender sender) : ControllerBase
{
    // Musteri "siparisim nerede" diye sorar; elinde takip numarasi degil
    // siparis id'si vardir.
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var requesterId = User.GetUserId();

        if (requesterId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new GetShipmentByOrderQuery(orderId, requesterId.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }

    // Servisin vitrin ucu: takip numarasiyla sorgulama.
    [HttpGet("track/{trackingNumber}")]
    public async Task<IActionResult> Track(string trackingNumber, CancellationToken cancellationToken)
    {
        var requesterId = User.GetUserId();

        if (requesterId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new GetShipmentByTrackingQuery(trackingNumber, requesterId.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }
}
