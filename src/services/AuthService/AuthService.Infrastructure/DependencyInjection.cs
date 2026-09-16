using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.AuthService.Infrastructure.Authentication;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;
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

        // JWT DOGRULAMA ortak katmanda: her servis ayni kodu kullanir.
        services.AddJwtAuthentication(configuration);

        // Token URETIMI Auth Service'e ozel: sadece o token dagitir.
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

}
