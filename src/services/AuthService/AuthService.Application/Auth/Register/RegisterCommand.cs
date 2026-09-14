using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Auth.Register;

// Komut = emir kipi, gecmis zaman degil (o event olurdu).
// IRequest<T>: MediatR'a "bunu isleyen bir handler var, T donuyor" der.
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Role) : IRequest<Result<Guid>>;
