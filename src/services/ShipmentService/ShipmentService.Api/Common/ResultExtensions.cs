using Microsoft.AspNetCore.Mvc;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.ShipmentService.Api.Common;

// Result -> HTTP cevirisi TEK YERDE. Controller'lar "hangi hata hangi status
// kodu" bilgisini tasimaz; yeni bir hata kodu eklendiginde degisen tek dosya burasi.
public static class ResultExtensions
{
    public static IActionResult ToProblem(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Basarili sonuc icin problem uretilemez.");
        }

        var status = result.Error.Code switch
        {
            "shipment.not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        // ProblemDetails: RFC 9457 standardi. Her servis hatayi ayni formatta
        // dondurunce frontend tek bir hata isleyici yazar.
        return controller.Problem(
            statusCode: status,
            title: result.Error.Code,
            detail: result.Error.Message);
    }
}
