using System.Security.Claims;

namespace SmartLogistics.BuildingBlocks.Infrastructure.Authentication;

// Token'in sahibini okumanin tek yolu. Her controller'da tekrar eden
// "sub claim'ini bul, Guid'e cevir" kodunu ortadan kaldirir.
//
// Neden onemli: kullanici kimligi ISTEMCIDEN GELMEZ. Istemcinin gonderdigi
// customerId'ye guvenseydik, bir musteri baskasinin adina siparis acabilirdi.
// Tek guvenilir kaynak imzali token.
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst(JwtClaimNames.Subject)?.Value;

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}
