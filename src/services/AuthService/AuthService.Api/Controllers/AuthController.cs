using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLogistics.AuthService.Api.Common;
using SmartLogistics.AuthService.Application.Auth.Login;
using SmartLogistics.AuthService.Application.Auth.Me;
using SmartLogistics.AuthService.Application.Auth.Register;

namespace SmartLogistics.AuthService.Api.Controllers;

// Controller INCE olmali: is kurali yok, dogrulama yok. Sadece komutu
// mediator'a ver, donen Result'i HTTP'ye cevir.
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(new { userId = result.Value })
            : result.ToProblem(this);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }

    // [Authorize]: gecerli bir JWT yoksa handler'a hic ulasilmaz, 401 doner.
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        // "sub" claim'i token'in sahibini soyler. MapInboundClaims=false
        // oldugu icin claim adi token'da yazdigi gibi duruyor.
        var subject = User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(this);
    }
}
