using SmartLogistics.TestSupport;

namespace SmartLogistics.OrderService.IntegrationTests.Fixtures;

// Bu testler HEM veritabani HEM broker istiyor. Bir koleksiyon birden
// fazla fixture tasiyabilir; ikisi de sinif basina degil koleksiyon
// basina bir kez kalkar.
[CollectionDefinition(nameof(InfrastructureCollection))]
public sealed class InfrastructureCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<RabbitMqFixture>;
