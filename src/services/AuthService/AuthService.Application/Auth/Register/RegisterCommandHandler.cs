using MediatR;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Auth.Register;

// Handler INCE: dogrulama ValidationBehavior'da, Identity detaylari
// IIdentityService'in arkasinda. Burada sadece use-case akisi var.
public sealed class RegisterCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.Role,
            cancellationToken);
}
