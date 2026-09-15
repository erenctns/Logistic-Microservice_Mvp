using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;
using SmartLogistics.BuildingBlocks.Infrastructure.Messaging;
using SmartLogistics.OrderService.Application.Common.Interfaces;
using SmartLogistics.OrderService.Infrastructure.Persistence;

namespace SmartLogistics.OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("OrderDb"))
            .UseSnakeCaseNamingConvention());

        // Application'da TANIMLANAN sozlesmeler, burada EF Core ile karsilaniyor.
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Token DOGRULAMA ortak katmandan. Order Service token uretmez;
        // gelen token'in imzasini kendisi dogrular, Auth'a istek atmaz.
        services.AddJwtAuthentication(configuration);

        services.AddEventBus(configuration, bus =>
        {
            // OUTBOX. Bu blok olmasaydi Publish cagrisi mesaji DOGRUDAN
            // RabbitMQ'ya gonderirdi ve dual write problemi geri gelirdi:
            // veritabani commit'i ile broker'a gonderim arasinda sistem
            // olurse siparis var ama event yok.
            //
            // UseBusOutbox ile Publish, mesaji outbox_message TABLOSUNA
            // yaziyor — yani is verisiyle AYNI transaction'in icine giriyor.
            // Commit basariliysa iki satir da var, degilse ikisi de yok.
            //
            // Commit'ten sonra MassTransit'in arka plan servisi tabloyu
            // tarayip mesaji broker'a basiyor ve satiri temizliyor.
            bus.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
        });

        return services;
    }
}
