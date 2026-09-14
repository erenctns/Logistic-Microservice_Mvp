using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.AuthService.Infrastructure.Authentication;
using SmartLogistics.AuthService.Infrastructure.Identity;
using SmartLogistics.AuthService.Infrastructure.Persistence;

namespace SmartLogistics.AuthService.Infrastructure;

// Infrastructure katmaninin DI kayitlari: Application'da TANIMLANAN
// interface'lerin somut karsiliklari burada baglanir.
//
// IConfiguration neden burada? Connection string, RabbitMQ host'u gibi
// ayarlari okuyacak katman bu. Application bir ayar bile okumaz —
// is kurali "veritabani nerede" sorusunu umursamaz.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("AuthDb"))
            // Identity'nin PascalCase tablolarini snake_case'e cevirir.
            .UseSnakeCaseNamingConvention());

        // AddIdentityCore: cookie/UI katmani olmadan sadece kullanici ve rol
        // yonetimi. API'yiz; oturumu JWT tasiyacak, cookie'ye ihtiyac yok.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                // Ozel karakter zorunlulugu kullaniciyi "Parola1!" gibi tahmin
                // edilebilir kaliplara itiyor; uzunluk daha etkili bir onlem.
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                // Kaba kuvvet denemesine karsi hesap kilitleme.
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>();

        // Application'da TANIMLANAN sozlesme, burada Identity ile karsilaniyor.
        services.AddScoped<IIdentityService, IdentityService>();

        AddJwt(services, configuration);

        // Step 06: IEventBus -> RabbitMqEventBus
        // Step 07: OutboxPublisher (BackgroundService)
        return services;
    }

    private static void AddJwt(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt yapilandirmasi eksik (Jwt__Key, Jwt__Issuer...).");

        // Acilista dogrula, ilk istekte degil: HS256 icin 32 karakterden kisa
        // anahtar kutuphane tarafindan reddedilir ve hata calisma aninda cikar.
        if (options.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt__Key en az 32 karakter olmali (HS256 = 256 bit).");
        }

        services.AddSingleton(options);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Gelen istekteki "Authorization: Bearer ..." basligini dogrular.
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

                    // Suresi dolmus token reddedilir.
                    ValidateLifetime = true,
                    // Varsayilan 5 dakikalik tolerans; suresi dolan token
                    // 5 dakika daha gecerli kalmasin.
                    ClockSkew = TimeSpan.Zero,

                    // Token'daki "role" claim'i rol olarak degerlendirilsin.
                    RoleClaimType = JwtTokenGenerator.RoleClaimName,
                };

                // Varsayilan davranis "sub" claim'ini uzun bir URI'ye cevirir.
                // Kapatiyoruz: token'da ne yaziyorsa kodda da o gorunsun.
                bearer.MapInboundClaims = false;
            });

        // UseAuthorization() middleware'i bu kayit olmadan calismaz.
        // [Authorize] attribute'unu degerlendiren politika motorunu kurar.
        services.AddAuthorization();
    }
}
