using MediatR;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Auth.Login;

// Istemciye donen cevap. Token'in yaninda son kullanma zamanini da
// veriyoruz ki frontend suresi dolmadan yenileme/cikis karari verebilsin.
public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
