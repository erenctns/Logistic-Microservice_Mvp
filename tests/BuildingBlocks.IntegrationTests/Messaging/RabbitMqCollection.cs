using SmartLogistics.TestSupport;

namespace SmartLogistics.BuildingBlocks.IntegrationTests.Messaging;

// Container pahali (~5 sn). Bu koleksiyondaki tum test siniflari AYNI
// RabbitMqFixture ornegini, dolayisiyla ayni broker'i paylasir.
//
// PostgresCollection'da oldugu gibi: fixture TestSupport'ta paylasilir,
// koleksiyon tanimi her test assembly'sinde tekrar yazilir (xUnit1041).
[CollectionDefinition(nameof(RabbitMqCollection))]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>;
