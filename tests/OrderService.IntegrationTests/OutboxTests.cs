using AwesomeAssertions;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartLogistics.Contracts.Events;
using SmartLogistics.OrderService.Application;
using SmartLogistics.OrderService.Application.Orders.Create;
using SmartLogistics.OrderService.Domain;
using SmartLogistics.BuildingBlocks.Infrastructure.Messaging;
using SmartLogistics.OrderService.Infrastructure;
using SmartLogistics.OrderService.Infrastructure.Persistence;
using SmartLogistics.TestSupport;

namespace SmartLogistics.OrderService.IntegrationTests;

// ⭐ ADIMIN VITRIN TESTLERI: outbox pattern gercekten calisiyor mu?
//
// Gercek PostgreSQL ve gercek RabbitMQ kullaniliyor. Bu davranis in-memory
// bir veritabaniyla DOGRULANAMAZ: test edilen sey tam olarak transaction
// semantigi.
[Collection(nameof(Fixtures.InfrastructureCollection))]
public class OutboxTests(PostgresFixture postgres, RabbitMqFixture rabbit)
{
    private static readonly Guid Customer = Guid.NewGuid();

    // Broker ADRESI parametrik: bazi testlerde bilerek ulasilamaz bir port
    // veriyoruz — "RabbitMQ cokmus" senaryosunu boyle canlandiriyoruz.
    private async Task<ServiceProvider> BuildAsync(
        string rabbitHost,
        ushort rabbitPort,
        Action<IServiceCollection>? extra = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrderDb"] = postgres.ConnectionString,
                ["Jwt:Key"] = "integration-test-only-signing-key-32chars",
                ["Jwt:Issuer"] = "smart-logistics-test",
                ["Jwt:Audience"] = "smart-logistics-test-clients",
                ["RabbitMq:Host"] = rabbitHost,
                ["RabbitMq:Port"] = rabbitPort.ToString(),
                ["RabbitMq:Username"] = rabbit.Username,
                ["RabbitMq:Password"] = rabbit.Password,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        extra?.Invoke(services);

        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await database.Database.MigrateAsync();

        // Her test temiz sayfadan baslasin.
        await database.Database.ExecuteSqlRawAsync("DELETE FROM outbox_message; DELETE FROM orders;");

        return provider;
    }

    private static CreateOrderCommand NewOrder() =>
        new(Customer, "Kadikoy, Istanbul", PackageSize.Medium);

    [Fact]
    public async Task CreateOrder_WhenBrokerIsUnreachable_StillWritesOrderAndOutboxRow()
    {
        // Broker'i ulasilamaz bir porta yonlendiriyoruz ve bus'i hic
        // baslatmiyoruz: yani RabbitMQ cokmus durumda.
        var provider = await BuildAsync("localhost", 59999);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(NewOrder(), cancellationToken);

        result.IsSuccess.Should().BeTrue("broker cokse bile siparis alinabilmeli");

        var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        // Siparis yazildi...
        (await database.Orders.CountAsync(cancellationToken)).Should().Be(1);

        // ...VE event bir veritabani satiri olarak bekliyor.
        // Broker dondugunde buradan gonderilecek; hicbir event kaybolmadi.
        var pending = await database.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM outbox_message")
            .SingleAsync(cancellationToken);

        pending.Should().Be(1, "event outbox tablosunda beklemeli");

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_WhenTransactionRollsBack_WritesNeitherOrderNorOutboxRow()
    {
        // ATOMIKLIGIN KANITI: ikisi de ayni transaction'da oldugu icin
        // geri alma HER IKISINI birden siliyor. Farkli transaction'larda
        // olsalardi biri kalirdi ve sistem tutarsiz olurdu.
        var provider = await BuildAsync("localhost", 59999);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await using (var transaction = await database.Database.BeginTransactionAsync(cancellationToken))
        {
            await sender.Send(NewOrder(), cancellationToken);

            await transaction.RollbackAsync(cancellationToken);
        }

        (await database.Orders.CountAsync(cancellationToken)).Should().Be(0);

        var pending = await database.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM outbox_message")
            .SingleAsync(cancellationToken);

        pending.Should().Be(0, "siparis geri alindiysa event de geri alinmali");

        await provider.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_WhenBrokerIsAvailable_EventReachesAnotherService()
    {
        // Ucten uca: outbox satiri yaziliyor, MassTransit'in arka plan servisi
        // onu broker'a basiyor, BASKA BIR SERVIS aliyor.
        //
        // Consumer'i ayri bir DI kabinda kuruyoruz — cunku gercekte de ayri
        // bir process'te (Step 08'de Shipment Service) yasayacak. Ayni kapta
        // iki bus kurulamaz zaten: AddMassTransit container basina bir kez.
        var received = new TaskCompletionSource<OrderCreated>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationToken = TestContext.Current.CancellationToken;

        var orderService = await BuildAsync(rabbit.Host, rabbit.Port);
        var consumerService = BuildConsumerService(received);

        // DIKKAT: sadece IBusControl'u baslatmak YETMIYOR.
        // Outbox satirini surup broker'a basan sey MassTransit'in arka plan
        // servisi (IHostedService). Uygulamada onu ASP.NET host'u baslatiyor;
        // elle kurdugumuz DI kabinda kimse baslatmadigi icin mesaj tabloda
        // kaliyordu. Burada host'un isini elle yapiyoruz.
        await StartHostedServicesAsync(orderService, cancellationToken);
        await StartHostedServicesAsync(consumerService, cancellationToken);

        try
        {
            await using var scope = orderService.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(NewOrder(), cancellationToken);

            var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellationToken);

            message.DeliveryAddress.Should().Be("Kadikoy, Istanbul");
            message.PackageSize.Should().Be("Medium");
            message.CustomerId.Should().Be(Customer);
        }
        finally
        {
            await StopHostedServicesAsync(consumerService);
            await StopHostedServicesAsync(orderService);
            await consumerService.DisposeAsync();
            await orderService.DisposeAsync();
        }
    }

    // "Baska servis" taklidi: sadece bus ve bir consumer, veritabani yok.
    private ServiceProvider BuildConsumerService(TaskCompletionSource<OrderCreated> received)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = rabbit.Host,
                ["RabbitMq:Port"] = rabbit.Port.ToString(),
                ["RabbitMq:Username"] = rabbit.Username,
                ["RabbitMq:Password"] = rabbit.Password,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(received);
        services.AddEventBus(configuration, bus => bus.AddConsumer<OrderCreatedTestConsumer>());

        return services.BuildServiceProvider();
    }

    private static async Task StartHostedServicesAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cancellationToken);
        }
    }

    private static async Task StopHostedServicesAsync(IServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    public sealed class OrderCreatedTestConsumer(TaskCompletionSource<OrderCreated> received)
        : IConsumer<OrderCreated>
    {
        public Task Consume(ConsumeContext<OrderCreated> context)
        {
            received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }
}
