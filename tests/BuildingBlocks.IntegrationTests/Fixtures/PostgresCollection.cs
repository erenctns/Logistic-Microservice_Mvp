using SmartLogistics.TestSupport;

namespace SmartLogistics.BuildingBlocks.IntegrationTests.Fixtures;

// NEDEN BURADA, TestSupport'ta DEGIL?
// xUnit koleksiyon tanimlarini SADECE kendi assembly'sinde arar; baska bir
// projedeki [CollectionDefinition] gorunmez (xUnit1041). Bu yuzden paylasilan
// sey FIXTURE sinifi (TestSupport'ta), her test projesi de kendi 3 satirlik
// tanimini yazar. Her servis testinde ayni 3 satir tekrar eder.
//
// Bu koleksiyondaki tum siniflar ayni PostgresFixture ornegini, dolayisiyla
// ayni container'i paylasir; buna karsilik birbirleriyle paralel calismazlar.
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
