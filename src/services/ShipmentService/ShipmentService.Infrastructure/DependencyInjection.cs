using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;
using SmartLogistics.BuildingBlocks.Infrastructure.Messaging;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Infrastructure.Messaging;
using SmartLogistics.ShipmentService.Infrastructure.Persistence;

namespace SmartLogistics.ShipmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ShipmentDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("ShipmentDb"))
            .UseSnakeCaseNamingConvention());

        // Application'da TANIMLANAN sozlesmeler, burada EF Core ile karsilaniyor.
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITrackingNumberGenerator, TrackingNumberGenerator>();

        // Token DOGRULAMA ortak katmandan; bu servis de token uretmez.
        services.AddJwtAuthentication(configuration);

        services.AddEventBus(configuration, bus =>
        {
            // Bu tek satir kuyrugu doguruyor: MassTransit acilista
            // "order-created" kuyrugunu olusturup OrderCreated exchange'ine
            // bagliyor. Sinif adindan kebab-case'e cevrilen ad bu.
            bus.AddConsumer<OrderCreatedConsumer>();

            bus.AddEntityFrameworkOutbox<ShipmentDbContext>(outbox =>
            {
                outbox.UsePostgres();

                // YAYIN tarafi (Order Service'teki ile ayni): consumer'in
                // yayinladigi ShipmentCreated dogrudan broker'a degil
                // outbox_message tablosuna gidiyor.
                outbox.UseBusOutbox();
            });

            // ⭐ TUKETIM tarafi — INBOX. Order Service'te OLMAYAN parca.
            //
            // Her receive endpoint'ine bir filtre takiyor. O filtre mesaji
            // consumer'a vermeden once inbox_state tablosuna (MessageId,
            // ConsumerId) satirini yazmayi deniyor:
            //
            //   satir zaten varsa -> consumer HIC calismaz, mesaj ack'lenir
            //   satir yoksa       -> transaction acilir, consumer calisir,
            //                        inbox + is verisi + giden event
            //                        BIRLIKTE commit edilir
            //
            // Boylece at-least-once TESLIMAT, exactly-once ISLEME'ye
            // donusuyor. Callback kullanmamizin sebebi: kuyruklari tek tek
            // elle tanimlamadan (ConfigureEndpoints otomatik kuruyor)
            // hepsine ayni filtreyi takabilmek.
            bus.AddConfigureEndpointsCallback((context, _, cfg) =>
                cfg.UseEntityFrameworkOutbox<ShipmentDbContext>(context));
        });

        return services;
    }
}
