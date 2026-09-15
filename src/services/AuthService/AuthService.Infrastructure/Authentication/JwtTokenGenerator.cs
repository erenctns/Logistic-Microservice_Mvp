using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;

namespace SmartLogistics.AuthService.Infrastructure.Authentication;

// Token uc parcadir: header.payload.signature
//   payload  -> base64, SIFRELI DEGIL. Herkes okuyabilir, gizli bilgi konmaz.
//   signature-> HMACSHA256(header + payload, Key). Anahtari sadece sunucu bilir.
//
// Sonuc: payload'i degistiren (ornegin rolunu Admin yapan) biri imzayi bozar,
// dogrulama basarisiz olur. Sunucu hicbir oturum saklamaz — token'in kendisi
// kanittir. Mikroserviste kritik: diger servisler "bu kim?" diye Auth'a
// sormadan, imzayi dogrulayip icindeki role bakarak karar verir.
public sealed class JwtTokenGenerator(JwtOptions options) : IJwtTokenGenerator
{
    public AccessToken Generate(
        Guid userId,
        string email,
        string fullName,
        IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            // sub: token'in sahibi. Diger servisler kullanici kimligini buradan okur.
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            // jti: token'in tekil kimligi (ileride iptal listesi icin gerekir).
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Rol claim'leri: [Authorize(Roles = "Admin")] bunlara bakar.
        // ClaimTypes.Role uzun bir schemas.microsoft.com URI'si uretir; kisa ad okunur kalir.
        claims.AddRange(roles.Select(role => new Claim(JwtClaimNames.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }
}
