using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;
using SmartLogistics.Contracts;
using SmartLogistics.OrderService.Api.Common;
using SmartLogistics.OrderService.Application.Orders.Create;
using SmartLogistics.OrderService.Application.Orders.GetById;
using SmartLogistics.OrderService.Application.Orders.GetMine;

namespace SmartLogistics.OrderService.Api.Controllers;

// [Authorize] sinif seviyesinde: bu controller'daki HICBIR endpoint
// token'siz calismaz. Tek tek yazmak yerine varsayilani guvenli yapiyoruz.
[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    // Istemcinin gonderdigi govdede customerId YOK; token'dan okunuyor.
    public sealed record CreateOrderRequest(string DeliveryAddress, string PackageSize);

    // Sadece musteri siparis olusturabilir. Kurye veya admin token'i
    // gecerli olsa bile bu endpoint'e giremez.
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();

        if (customerId is null)
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<Domain.PackageSize>(request.PackageSize, ignoreCase: true, out var packageSize))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "order.invalid_package_size",
                detail: "PackageSize su degerlerden biri olmali: Small, Medium, Large.");
        }

        var result = await sender.Send(
            new CreateOrderCommand(customerId.Value, request.DeliveryAddress, packageSize),
            cancellationToken);

        return result.IsSuccess
            // 201 Created + Location basligi: REST'te yeni kaynak boyle bildirilir.
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { orderId = result.Value })
            : result.ToProblem(this);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var requesterId = User.GetUserId();

        if (requesterId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetOrderByIdQuery(id, requesterId.Value), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();

        if (customerId is null)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetMyOrdersQuery(customerId.Value), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }
}
