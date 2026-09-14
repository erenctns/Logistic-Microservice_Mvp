using SmartLogistics.TestSupport;

namespace SmartLogistics.AuthService.IntegrationTests.Fixtures;

// xUnit koleksiyon tanimlarini SADECE kendi assembly'sinde arar; bu yuzden
// paylasilan sey fixture SINIFI (TestSupport'ta), tanim ise her test
// projesinde tekrar yazilir (xUnit1041).
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
