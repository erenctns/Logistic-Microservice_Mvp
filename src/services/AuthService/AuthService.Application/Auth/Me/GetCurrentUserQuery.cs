using MediatR;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Auth.Me;

// Query = sorgu, sistemi DEGISTIRMEZ. CQRS'in "read" tarafi.
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserInfo>>;

public sealed class GetCurrentUserQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetCurrentUserQuery, Result<UserInfo>>
{
    public Task<Result<UserInfo>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        identityService.GetByIdAsync(request.UserId, cancellationToken);
}
