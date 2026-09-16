using AwesomeAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartLogistics.BuildingBlocks.Infrastructure.Messaging;
using SmartLogistics.Contracts.Events;
using SmartLogistics.ShipmentService.Application;
using SmartLogistics.ShipmentService.Infrastructure;
using SmartLogistics.ShipmentService.Infrastructure.Persistence;
using SmartLogistics.TestSupport;

namespace SmartLogistics.ShipmentService.IntegrationTests;

// ⭐ ADIMIN VITRIN TESTLERI: ayni mesaj iki kez gelirse ne oluyor?
//
// Gercek PostgreSQL ve gercek RabbitMQ kullaniliyor. Test edilen sey tam
// olarak transaction semantigi ve broker davranisi; in-memory taklitlerle
// DOGRULANAMAZ.
//
// Kurulum iki AYRI DI kabi:
//   1. Shipment Service  — consumer + inbox + outbox (gercek servis kodu)
//   2. "Dis dunya"       — OrderCreated yayinlar, ShipmentCreated dinler
//      (gercekte Order Service ve Delivery Service; testte tek kap)
//
// Ayri kaplar sart: AddMassTransit bir kapta yalnizca BIR kez cagrilabilir.
[Collection(nameof(Fixtures.InfrastructureCollection))]
public class InboxTests(PostgresFixture postgres, RabbitMqFixture rabbit)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    // ---------------------------------------------------------------
    //  Kurulum
    // ---------------------------------------------------------------

    private IConfiguration BuildConfiguration(bool withDatabase)
    {
        var settings = new Dictionary<string, string?>
        {
            ["RabbitMq:Host"] = rabbit.Host,
            ["RabbitMq:Port"] = rabbit.Port.ToString(),
            ["RabbitMq:Username"] = rabbit.Username,
            ["RabbitMq:Password"] = rabbit.Password,
        };

        if (withDatabase)
        {
            settings["ConnectionStrings:ShipmentDb"] = postgres.ConnectionString;
            settings["Jwt:Key"] = "integration-test-only-signing-key-32chars";
            settings["Jwt:Issuer"] = "smart-logistics-test";
            settings["Jwt:Audience"] = "smart-logistics-test-clients";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    // Gercek servis kodu: AddApplication + AddInfrastructure. Yani consumer,
    // inbox filtresi ve outbox uretimde nasil kuruluysa oyle kuruluyor.
    private async Task<ServiceProvider> BuildShipmentServiceAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(BuildConfiguration(withDatabase: true));
        WaitForBusOnStartup(services);

        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();
        await database.Database.MigrateAsync(cancellationToken);

        // Her test temiz sayfadan baslasin. Sira onemli: outbox_message'in
        // inbox_state'e foreign key'i var, once o silinmeli.
        await database.Database.ExecuteSqlRawAsync(
            "DELETE FROM outbox_message; DELETE FROM inbox_state; DELETE FROM shipments;",
            cancellationToken);

        // VERITABANINI temizlemek yetmiyor: kuyruklar broker container'i
        // ile birlikte yasiyor ve onceki testten artan bir mesaj bu testin
        // kabinda tuketilebilir. Iki durumlu sistemin IKISINI de sifirla.
        foreach (var queue in (string[])["order-created", "order-created_error", "shipment-created-test"])
        {
            await rabbit.PurgeQueueAsync(queue, cancellationToken);
        }

        return provider;
    }

    // Sistemin geri kalani: Order Service gibi yayinlar, Delivery Service
    // gibi dinler. Veritabani yok — sadece bus.
    private ServiceProvider BuildOutsideWorld(ShipmentCreatedRecorder recorder)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddEventBus(
            BuildConfiguration(withDatabase: false),
            bus => bus.AddConsumer<ShipmentCreatedTestConsumer>());
        WaitForBusOnStartup(services);

        return services.BuildServiceProvider();
    }

    // ⚠️ TESTLERI KARARSIZ YAPAN AYAR.
    //
    // MassTransit'in hosted service'i varsayilan olarak bus'i ARKA PLANDA
    // baslatir (WaitUntilStarted = false) ve StartAsync hemen doner.
    // Uygulamada bu dogru tercih: broker o an ulasilamazsa servis yine de
    // ayaga kalksin, arka planda baglanmayi denesin.
    //
    // Testte ise felaket: StartAsync dondu diye mesaj yayinliyoruz, ama
    // dinleyicinin kuyrugu HENUZ OLUSMAMIS olabiliyor. Fanout exchange'e
    // gelen mesajin bagli kuyrugu yoksa mesaj HICBIR YERDE BIRIKMEZ,
    // sessizce dusurulur. Test de "event gelmedi" diye 60 saniye bekler.
    //
    // Bu ayar StartAsync'i bus tamamen hazir olana kadar bekletir.
    private static void WaitForBusOnStartup(IServiceCollection services) =>
        services.Configure<MassTransitHostOptions>(options => options.WaitUntilStarted = true);

    // ---------------------------------------------------------------
    //  Testler
    // ---------------------------------------------------------------

    [Fact]
    public async Task Consume_WhenOrderCreatedArrives_CreatesShipmentAndPublishesShipmentCreated()
    {
        // ZINCIRIN KANITI: Order yayinlar -> Shipment tuketir -> Shipment
        // yayinlar -> bir baskasi tuketir. Kimse kimseyi cagirmiyor.
        var cancellationToken = TestContext.Current.CancellationToken;
        var order = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var recorder = new ShipmentCreatedRecorder();

        var shipmentService = await BuildShipmentServiceAsync(cancellationToken);
        var outsideWorld = BuildOutsideWorld(recorder);

        await StartAsync(shipmentService, outsideWorld, cancellationToken);

        try
        {
            await PublishOrderCreatedAsync(outsideWorld, order, customer, cancellationToken: cancellationToken);

            var arrived = await WaitUntilAsync(
                () => Task.FromResult(recorder.CountFor(order) == 1),
                cancellationToken);

            arrived.Should().BeTrue("ShipmentCreated zincirin ikinci halkasi olarak yayinlanmali");

            var published = recorder.FirstFor(order)!;
            published.CustomerId.Should().Be(customer);
            published.DeliveryAddress.Should().Be("Kadikoy, Istanbul");
            published.TrackingNumber.Should().StartWith("TR-");

            (await ShipmentCountAsync(shipmentService, order, cancellationToken)).Should().Be(1);
        }
        finally
        {
            await StopAsync(shipmentService, outsideWorld);
        }
    }

    [Fact]
    public async Task Consume_WhenSameMessageArrivesTwice_CreatesSingleShipment()
    {
        // ⭐ INBOX'IN KANITI.
        //
        // Ayni MessageId ikinci kez geldiginde consumer HIC calismiyor.
        // Bu, outbox'in at-least-once garantisinin karsiligi: mesaj iki kez
        // teslim edilebilir, ama etkisi bir kez olur.
        var cancellationToken = TestContext.Current.CancellationToken;
        var order = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var recorder = new ShipmentCreatedRecorder();

        var shipmentService = await BuildShipmentServiceAsync(cancellationToken);
        var outsideWorld = BuildOutsideWorld(recorder);

        await StartAsync(shipmentService, outsideWorld, cancellationToken);

        try
        {
            await PublishOrderCreatedAsync(outsideWorld, order, customer, messageId, cancellationToken);

            // Once ILK mesajin TAMAMEN oturmasini bekliyoruz: gonderi
            // yazilmis, giden event teslim edilmis, outbox bosalmis.
            // Boylece sonraki adimda gordugumuz hareket kesinlikle IKINCI
            // mesaja ait olur — yoksa arka plan surecleriyle karisirdi.
            var settled = await WaitUntilAsync(
                async () => await ShipmentCountAsync(shipmentService, order, cancellationToken) == 1
                    && await InboxDeliveredCountAsync(shipmentService, messageId, cancellationToken) == 1
                    && await OutboxCountAsync(shipmentService, messageId, cancellationToken) == 0,
                cancellationToken);

            settled.Should().BeTrue("ilk mesaj sonuna kadar islenmeli");

            var receiveCountBefore = await InboxReceiveCountAsync(shipmentService, messageId, cancellationToken);

            // AYNI MessageId ile ikinci kez.
            await PublishOrderCreatedAsync(outsideWorld, order, customer, messageId, cancellationToken);

            // receive_count artiyor: mesaj broker'dan GELDI ve inbox onu gordu.
            var secondDeliverySeen = await WaitUntilAsync(
                async () => await InboxReceiveCountAsync(shipmentService, messageId, cancellationToken)
                    > receiveCountBefore,
                cancellationToken);

            secondDeliverySeen.Should().BeTrue("ikinci teslimat inbox tarafindan goruldu");

            // ...ama consumer calismadi:
            (await ShipmentCountAsync(shipmentService, order, cancellationToken))
                .Should().Be(1, "ayni mesaj ikinci gonderi olusturmamali");

            (await InboxRowCountAsync(shipmentService, cancellationToken, messageId))
                .Should().Be(1, "ayni MessageId tek bir inbox satiri tutar");

            recorder.CountFor(order).Should().Be(1, "ikinci bir ShipmentCreated yayinlanmamali");
        }
        finally
        {
            await StopAsync(shipmentService, outsideWorld);
        }
    }

    [Fact]
    public async Task Consume_WhenDifferentMessageCarriesSameOrder_StillCreatesSingleShipment()
    {
        // ⭐ INBOX'IN YAKALAYAMADIGI DURUM.
        //
        // MessageId'ler FARKLI oldugu icin inbox iki ayri satir aciyor ve
        // consumer IKI KEZ calisiyor. Buna ragmen tek gonderi olusuyor —
        // cunku ikinci savunma hatti is katmaninda: "bu siparisin gonderisi
        // zaten var mi" kontrolu ve order_id UNIQUE index'i.
        //
        // Iki katmanin farki tam olarak burada gorunur: biri TEKNIK
        // tekrari, digeri MANTIKSAL tekrari yakalar.
        var cancellationToken = TestContext.Current.CancellationToken;
        var order = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var firstMessage = Guid.NewGuid();
        var secondMessage = Guid.NewGuid();
        var recorder = new ShipmentCreatedRecorder();

        var shipmentService = await BuildShipmentServiceAsync(cancellationToken);
        var outsideWorld = BuildOutsideWorld(recorder);

        await StartAsync(shipmentService, outsideWorld, cancellationToken);

        try
        {
            await PublishOrderCreatedAsync(outsideWorld, order, customer, firstMessage, cancellationToken);

            var first = await WaitUntilAsync(
                async () => await InboxDeliveredCountAsync(shipmentService, firstMessage, cancellationToken) == 1,
                cancellationToken);

            first.Should().BeTrue();

            // Ayni siparis, FARKLI MessageId.
            await PublishOrderCreatedAsync(outsideWorld, order, customer, secondMessage, cancellationToken);

            // Iki inbox satiri = iki mesaj islendi. Inbox devreye girmedi.
            var bothProcessed = await WaitUntilAsync(
                async () => await InboxRowCountAsync(
                    shipmentService, cancellationToken, firstMessage, secondMessage) == 2,
                cancellationToken);

            bothProcessed.Should().BeTrue("farkli MessageId iki ayri inbox satiri acar");

            (await ShipmentCountAsync(shipmentService, order, cancellationToken))
                .Should().Be(1, "is kurali ikinci gonderiyi engellemeli");

            recorder.CountFor(order).Should().Be(1, "Delivery Service ayni gonderi icin iki kez tetiklenmemeli");
        }
        finally
        {
            await StopAsync(shipmentService, outsideWorld);
        }
    }

    [Fact]
    public async Task Consume_WhenMessageIsInvalid_WritesNothingAndMovesItToTheErrorQueue()
    {
        // BOZUK MESAJ: gecersiz adres -> domain reddeder -> consumer
        // exception firlatir -> 3 retry -> order-created_error.
        //
        // Ayrica CONSUMER TARAFI ATOMIKLIGININ kaniti: consumer patlayinca
        // ne gonderi kaliyor ne de "islendi" damgasi. Yarim is yok.
        var cancellationToken = TestContext.Current.CancellationToken;
        var order = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var recorder = new ShipmentCreatedRecorder();

        var shipmentService = await BuildShipmentServiceAsync(cancellationToken);
        var outsideWorld = BuildOutsideWorld(recorder);

        await StartAsync(shipmentService, outsideWorld, cancellationToken);

        try
        {
            await PublishOrderCreatedAsync(
                outsideWorld,
                order,
                Guid.NewGuid(),
                messageId,
                cancellationToken,
                deliveryAddress: "   ");

            var movedToErrorQueue = await WaitUntilAsync(
                async () => await rabbit.GetMessageCountAsync("order-created_error", cancellationToken) == 1,
                cancellationToken);

            movedToErrorQueue.Should().BeTrue("3 denemeden sonra mesaj hata kuyruguna tasinmali");

            (await ShipmentCountAsync(shipmentService, order, cancellationToken))
                .Should().Be(0, "gecersiz mesajdan gonderi olusmamali");

            (await InboxDeliveredCountAsync(shipmentService, messageId, cancellationToken))
                .Should().Be(0, "basarisiz consume islendi olarak damgalanmamali");

            recorder.CountFor(order).Should().Be(0);
        }
        finally
        {
            await StopAsync(shipmentService, outsideWorld);
        }
    }

    // ---------------------------------------------------------------
    //  Yardimcilar
    // ---------------------------------------------------------------

    private static Task PublishOrderCreatedAsync(
        ServiceProvider outsideWorld,
        Guid orderId,
        Guid customerId,
        Guid? messageId = null,
        CancellationToken cancellationToken = default,
        string deliveryAddress = "Kadikoy, Istanbul")
    {
        var message = new OrderCreated(orderId, customerId, deliveryAddress, "Medium", DateTime.UtcNow);

        // MessageId'yi ELLE veriyoruz: idempotency'nin anahtari bu.
        // Normalde MassTransit her yayinda yeni bir tane uretir; burada
        // "ayni mesaj tekrar geldi" durumunu boyle canlandiriyoruz.
        return outsideWorld.GetRequiredService<IBus>().Publish(
            message,
            context => context.MessageId = messageId ?? Guid.NewGuid(),
            cancellationToken);
    }

    // DIKKAT: IBusControl.StartAsync YETMEZ.
    // Outbox'i suren ve kuyruklari acan sey hosted service'lerdir;
    // uygulamada onlari ASP.NET host'u baslatir, burada biz baslatiyoruz.
    private static async Task StartAsync(
        ServiceProvider first,
        ServiceProvider second,
        CancellationToken cancellationToken)
    {
        foreach (var provider in new[] { first, second })
        {
            foreach (var hostedService in provider.GetServices<IHostedService>())
            {
                await hostedService.StartAsync(cancellationToken);
            }
        }
    }

    private static async Task StopAsync(ServiceProvider first, ServiceProvider second)
    {
        foreach (var provider in new[] { second, first })
        {
            foreach (var hostedService in provider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(CancellationToken.None);
            }

            await provider.DisposeAsync();
        }
    }

    // TUM sayimlar testin KENDI kimliklerine daraltilmis durumda.
    //
    // Sebebi: Postgres ve RabbitMQ container'lari tum sinif boyunca
    // paylasiliyor. Genel bir "COUNT(*) FROM shipments" yazsaydik, baska
    // bir testten sizan tek bir mesaj testi dusururdu — ve hata mesaji
    // gercek sebebi gostermezdi. Dar sorgu, testi kendi verisine kilitler.
    private static Task<int> ShipmentCountAsync(
        ServiceProvider provider,
        Guid orderId,
        CancellationToken cancellationToken) =>
        ScalarAsync(
            provider,
            "SELECT COUNT(*)::int AS \"Value\" FROM shipments WHERE order_id = {0}",
            cancellationToken,
            orderId);

    private static Task<int> InboxRowCountAsync(
        ServiceProvider provider,
        CancellationToken cancellationToken,
        params Guid[] messageIds) =>
        ScalarAsync(
            provider,
            "SELECT COUNT(*)::int AS \"Value\" FROM inbox_state WHERE message_id = ANY({0})",
            cancellationToken,
            messageIds);

    private static Task<int> InboxDeliveredCountAsync(
        ServiceProvider provider,
        Guid messageId,
        CancellationToken cancellationToken) =>
        ScalarAsync(
            provider,
            "SELECT COUNT(*)::int AS \"Value\" FROM inbox_state WHERE message_id = {0} AND consumed IS NOT NULL",
            cancellationToken,
            messageId);

    private static Task<int> InboxReceiveCountAsync(
        ServiceProvider provider,
        Guid messageId,
        CancellationToken cancellationToken) =>
        ScalarAsync(
            provider,
            "SELECT COALESCE(SUM(receive_count), 0)::int AS \"Value\" FROM inbox_state WHERE message_id = {0}",
            cancellationToken,
            messageId);

    private static Task<int> OutboxCountAsync(
        ServiceProvider provider,
        Guid messageId,
        CancellationToken cancellationToken) =>
        ScalarAsync(
            provider,
            "SELECT COUNT(*)::int AS \"Value\" FROM outbox_message WHERE inbox_message_id = {0}",
            cancellationToken,
            messageId);

    private static async Task<int> ScalarAsync(
        ServiceProvider provider,
        string sql,
        CancellationToken cancellationToken,
        params object[] parameters)
    {
        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();

        return await database.Database.SqlQueryRaw<int>(sql, parameters).SingleAsync(cancellationToken);
    }

    private static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> condition,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + Timeout;

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

    // ---------------------------------------------------------------
    //  Dis dunyanin consumer'i
    // ---------------------------------------------------------------

    // Yayinlanan ShipmentCreated'lari kaydeder. Sayiyi kontrol etmek onemli:
    // "tek gonderi olustu" yetmez, "tek event yayinlandi" da gerekir —
    // yoksa Delivery Service ayni is icin iki kez tetiklenirdi.
    //
    // Sayim SIPARIS BAZLI: baska bir testten sizan mesaj bu testin sonucunu
    // degistiremesin diye. Testler paylasilan bir broker kullaniyor.
    public sealed class ShipmentCreatedRecorder
    {
        private readonly List<ShipmentCreated> _received = [];

        public int CountFor(Guid orderId)
        {
            lock (_received)
            {
                return _received.Count(message => message.OrderId == orderId);
            }
        }

        public ShipmentCreated? FirstFor(Guid orderId)
        {
            lock (_received)
            {
                return _received.FirstOrDefault(message => message.OrderId == orderId);
            }
        }

        public void Record(ShipmentCreated message)
        {
            lock (_received)
            {
                _received.Add(message);
            }
        }
    }

    public sealed class ShipmentCreatedTestConsumer(ShipmentCreatedRecorder recorder)
        : IConsumer<ShipmentCreated>
    {
        public Task Consume(ConsumeContext<ShipmentCreated> context)
        {
            recorder.Record(context.Message);
            return Task.CompletedTask;
        }
    }
}
