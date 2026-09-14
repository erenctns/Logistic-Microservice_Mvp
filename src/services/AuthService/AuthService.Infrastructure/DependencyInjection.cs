using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        // Step 05: AuthDbContext (EF Core), ASP.NET Identity, IJwtTokenGenerator
        // Step 06: IEventBus -> RabbitMqEventBus
        // Step 07: OutboxPublisher (BackgroundService)
        return services;
    }
}
