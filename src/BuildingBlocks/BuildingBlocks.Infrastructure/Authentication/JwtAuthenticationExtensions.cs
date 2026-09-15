using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SmartLogistics.BuildingBlocks.Infrastructure.Authentication;

// Gelen istekteki "Authorization: Bearer ..." basligini dogrular.
// Alti servisin tamami bu metodu cagiracak; token URETEN sadece Auth Service.
//
// Mikroserviste kimlik dogrulamanin en buyuk kazanci burada gorunuyor:
// Order Service, "bu kullanici kim?" diye Auth Service'e HTTP istegi ATMIYOR.
// Imzayi paylasilan anahtarla kendisi dogruluyor. Auth Service tamamen
// cokse bile diger servisler kimlik dogrulamaya devam eder.
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt yapilandirmasi eksik (Jwt__Key, Jwt__Issuer...).");

        // Acilista dogrula, ilk istekte degil: kisa anahtar kutuphane
        // tarafindan reddedilir ve hata calisma aninda ortaya cikar.
        if (options.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt__Key en az 32 karakter olmali (HS256 = 256 bit).");
        }

        services.AddSingleton(options);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    // Imza gecerli mi — token'in degistirilmedigi buradan anlasilir.
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),

                    // Token'i biz mi urettik, bize mi gonderildi?
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,

                    // Suresi dolmus token reddedilir. Varsayilan 5 dakikalik
                    // tolerans kapatildi: suresi dolan token 5 dakika daha yasamasin.
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    RoleClaimType = JwtClaimNames.Role,
                };

                // Varsayilan davranis "sub" claim'ini uzun bir URI'ye cevirir.
                // Kapatiyoruz: token'da ne yaziyorsa kodda da o gorunsun.
                bearer.MapInboundClaims = false;
            });

        // UseAuthorization() middleware'i bu kayit olmadan calismaz.
        services.AddAuthorization();

        return services;
    }
}
