using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.BuildingBlocks.Application.Messaging;

namespace SmartLogistics.BuildingBlocks.Infrastructure.Messaging;

// Alti servisin de cagiracagi TEK kurulum noktasi. Bir servis sadece
// kendi consumer'larini "configure" callback'inde kaydeder; host, retry ve
// isimlendirme kurallari burada, tek yerde durur.
public static class MessagingExtensions
{
    public static IServiceCollection AddEventBus( //burası bağlantıyı kurma işlemlerini felan yapıcak
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException(
                "RabbitMq yapilandirmasi eksik (RabbitMq__Host, RabbitMq__Username...).");

        services.AddMassTransit(bus =>
        {
            // Kuyruk adlari: OrderCreatedConsumer -> "order-created".
            // Varsayilan PascalCase olurdu; kebab-case hem RabbitMQ
            // arayuzunde hem loglarda okunakli.
            bus.SetKebabCaseEndpointNameFormatter();

            // Servise ozel kayitlar: consumer'lar, saga'lar, outbox.
            configure?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                // GECICI hatalar icin yeniden deneme: 1sn -> 2sn -> 4sn.
                // Neden artan araliklar? Veritabani kilidi veya ag dalgalanmasi
                // gibi durumlar genelde kisa surede duzelir; hemen ust uste
                // denemek sistemi daha da zorlar.
                //
                // Uc deneme de basarisiz olursa mesaj <kuyruk>_error kuyruguna
                // dusuyor (DLQ). Orada sonsuza kadar bekler, kimse tuketmez;
                // bozuk mesajin sistemi kilitlemesini bu engelliyor.
                cfg.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4)));

                // Kayitli her consumer icin kuyrugu acar, exchange'e baglar.
                // Bu tek satir, elle yazsaydik ~120 satirlik consumer temel
                // sinifinin yerini tutuyor.
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, MassTransitEventBus>();

        return services;
    }
}
