using AwesomeAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.BuildingBlocks.Application.Messaging;
using SmartLogistics.BuildingBlocks.Infrastructure.Messaging;
using SmartLogistics.TestSupport;

namespace SmartLogistics.BuildingBlocks.IntegrationTests.Messaging;

// GERCEK RabbitMQ container'ina karsi calisir. Mock yok: exchange'ler
// gercekten aciliyor, mesaj gercekten serialize edilip aga cikiyor,
// retry ve _error kuyrugu RabbitMQ'nun kendi davranisi.
[Collection(nameof(RabbitMqCollection))]
public class EventBusTests(RabbitMqFixture fixture)
{
    private readonly MessageCollector _collector = new();

    // Bus'i test icin ayaga kaldirir. Uygulamada bunu ASP.NET host'u yapiyor;
    // burada elle kurup elle durduruyoruz.
    private async Task<AsyncServiceScope> StartBusAsync(
        Action<IBusRegistrationConfigurator> configure,
        CancellationToken cancellationToken)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = fixture.Host,
                ["RabbitMq:Port"] = fixture.Port.ToString(),
                ["RabbitMq:Username"] = fixture.Username,
                ["RabbitMq:Password"] = fixture.Password,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_collector);
        services.AddEventBus(configuration, configure);

        var provider = services.BuildServiceProvider();

        // IBusControl: bus'in acma/kapama kumandasi. Baslatilmadan once
        // hicbir kuyruk acilmaz, hicbir mesaj tuketilmez.
        await provider.GetRequiredService<IBusControl>().StartAsync(cancellationToken);

        return provider.CreateAsyncScope();
    }

    private static async Task StopBusAsync(AsyncServiceScope scope)
    {
        var bus = scope.ServiceProvider.GetRequiredService<IBusControl>();
        await bus.StopAsync(CancellationToken.None);
        await scope.DisposeAsync();
    }

    [Fact]
    public async Task Publish_WhenConsumerIsRegistered_MessageReachesConsumer()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await StartBusAsync(bus => bus.AddConsumer<PingConsumer>(), cancellationToken);

        try
        {
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var sent = new PingEvent(Guid.NewGuid(), "merhaba");

            await eventBus.PublishAsync(sent, cancellationToken);

            var received = await _collector.FirstConsumerReceived.WaitAsync(
                TimeSpan.FromSeconds(30), cancellationToken);

            received.Should().Be(sent);
        }
        finally
        {
            await StopBusAsync(scope);
        }
    }

    [Fact]
    public async Task Publish_WithTwoConsumers_DeliversACopyToEach()
    {
        // Publish = fan-out. Gonderen kimin dinledigini bilmez; dinleyen
        // her consumer KENDI kuyrugunda kendi kopyasini alir.
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await StartBusAsync(bus =>
        {
            bus.AddConsumer<PingConsumer>();
            bus.AddConsumer<AuditPingConsumer>();
        }, cancellationToken);

        try
        {
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var sent = new PingEvent(Guid.NewGuid(), "iki dinleyici");

            await eventBus.PublishAsync(sent, cancellationToken);

            var first = await _collector.FirstConsumerReceived.WaitAsync(
                TimeSpan.FromSeconds(30), cancellationToken);
            var second = await _collector.SecondConsumerReceived.WaitAsync(
                TimeSpan.FromSeconds(30), cancellationToken);

            first.Should().Be(sent);
            second.Should().Be(sent);
        }
        finally
        {
            await StopBusAsync(scope);
        }
    }

    [Fact]
    public async Task Consume_WhenHandlerAlwaysThrows_RetriesThenMovesMessageToErrorQueue()
    {
        // ⭐ Adimin vitrin testi: bozuk bir mesaj sonsuza kadar denenmemeli.
        // 1 ilk deneme + 3 retry (1sn, 2sn, 4sn) sonunda mesaj "boom_error"
        // kuyruguna tasiniyor ve ana kuyruk temizleniyor.
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await StartBusAsync(bus => bus.AddConsumer<BoomConsumer>(), cancellationToken);

        try
        {
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            await eventBus.PublishAsync(new BoomEvent(Guid.NewGuid()), cancellationToken);

            var movedToErrorQueue = await WaitUntilAsync(
                async () => await fixture.GetMessageCountAsync("boom_error", cancellationToken) > 0,
                TimeSpan.FromSeconds(60),
                cancellationToken);

            movedToErrorQueue.Should().BeTrue("basarisiz mesaj _error kuyruguna dusmeli");

            // Hepsi ayni mesajin denemeleri: 1 + 3 retry.
            _collector.BoomAttempts.Should().Be(4);

            // Ana kuyruk bosalmali: mesaj orada birikip sistemi kilitlemiyor.
            // Sayaci beklemeyle kontrol ediyoruz cunku management eklentisinin
            // istatistikleri anlik degil, periyodik toplaniyor.
            var mainQueueDrained = await WaitUntilAsync(
                async () => await fixture.GetMessageCountAsync("boom", cancellationToken) == 0,
                TimeSpan.FromSeconds(30),
                cancellationToken);

            mainQueueDrained.Should().BeTrue("mesaj _error'a tasindiktan sonra ana kuyruk bosalmali");
        }
        finally
        {
            await StopBusAsync(scope);
        }
    }

    private static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return false;
    }
}
