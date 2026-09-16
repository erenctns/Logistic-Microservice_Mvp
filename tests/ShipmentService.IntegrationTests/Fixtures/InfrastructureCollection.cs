using SmartLogistics.TestSupport;

namespace SmartLogistics.ShipmentService.IntegrationTests.Fixtures;

// Bu testler HEM veritabani HEM broker istiyor. Koleksiyon tanimi her test
// projesinde tekrar ediliyor: [CollectionDefinition] yalnizca KENDI
// assembly'sinde gorunur (xUnit1041), fixture siniflari ise TestSupport'ta
// paylasiliyor.
[CollectionDefinition(nameof(InfrastructureCollection))]
public sealed class InfrastructureCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<RabbitMqFixture>;
