using MediatR;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Auth.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator tokenGenerator)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await identityService.ValidateCredentialsAsync(
            request.Email, request.Password, cancellationToken);

        if (user.IsFailure)
        {
            // Hatayi oldugu gibi tasi: "e-posta yok" ile "sifre yanlis"
            // ayrimini zaten IIdentityService yapmiyor (bilgi sizdirmamak icin).
            return Result.Failure<AuthResponse>(user.Error);
        }

        var token = tokenGenerator.Generate(
            user.Value.Id, user.Value.Email, user.Value.FullName, user.Value.Roles);

        return new AuthResponse(
            token.Value,
            token.ExpiresAtUtc,
            user.Value.Id,
            user.Value.Email,
            user.Value.FullName,
            user.Value.Roles);
    }
}
