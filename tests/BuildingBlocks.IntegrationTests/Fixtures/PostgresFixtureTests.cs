using AwesomeAssertions;
using Npgsql;
using SmartLogistics.TestSupport;

namespace SmartLogistics.BuildingBlocks.IntegrationTests.Fixtures;

// Bu sinif is kurali test etmiyor — TEST ALTYAPISININ kendisini dogruluyor.
// "Container kalkiyor mu, gercek SQL calisiyor mu, Respawn temizliyor mu?"
// Step 05'ten itibaren gercek DbContext testleri bu fixture'in uzerine oturacak.
[Collection(nameof(PostgresCollection))]
public class PostgresFixtureTests(PostgresFixture fixture)
{
    // TestContext.Current.CancellationToken: testi iptal edilebilir kilar.
    // xUnit analyzer'i (xUnit1051) token kabul eden her cagriya bunu zorunlu
    // tutuyor — asili kalan bir test butun kosuyu kilitlemesin diye.
    [Fact]
    public async Task Connection_WhenFixtureStarted_AcceptsQueries()
    {
        await using var command = new NpgsqlCommand("SELECT 1", fixture.Connection);

        var result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        result.Should().Be(1);
    }

    [Fact]
    public async Task Container_Always_RunsTheExpectedPostgresVersion()
    {
        // Compose'daki imajla ayni major surum olmali; testin dogruladigi
        // veritabani ile calisan veritabani farkli olursa test gerceklikten kopar.
        await using var command = new NpgsqlCommand("SHOW server_version", fixture.Connection);

        var version = (string?)await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        version.Should().StartWith("17.");
    }

    [Fact]
    public async Task Table_WhenRowInserted_CanBeReadBack()
    {
        await ExecuteAsync("CREATE TABLE IF NOT EXISTS smoke_orders (id uuid PRIMARY KEY, city text NOT NULL)");
        await ExecuteAsync("INSERT INTO smoke_orders (id, city) VALUES (gen_random_uuid(), 'Kadikoy')");

        await using var command = new NpgsqlCommand("SELECT city FROM smoke_orders LIMIT 1", fixture.Connection);
        var city = (string?)await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        city.Should().Be("Kadikoy");
    }

    [Fact]
    public async Task ResetAsync_AfterRowsInserted_EmptiesTableButKeepsSchema()
    {
        await ExecuteAsync("CREATE TABLE IF NOT EXISTS smoke_reset (id int)");
        await ExecuteAsync("INSERT INTO smoke_reset (id) VALUES (1), (2), (3)");
        (await CountAsync("smoke_reset")).Should().Be(3);

        await fixture.ResetAsync();

        // Satirlar gitti...
        (await CountAsync("smoke_reset")).Should().Be(0);

        // ...ama tablo duruyor: Respawn DELETE eder, DROP etmez.
        // Migration'lari tekrar calistirmadigi icin hizli olmasinin sebebi bu.
        await using var exists = new NpgsqlCommand(
            "SELECT to_regclass('public.smoke_reset') IS NOT NULL", fixture.Connection);
        (await exists.ExecuteScalarAsync(TestContext.Current.CancellationToken)).Should().Be(true);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = new NpgsqlCommand(sql, fixture.Connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> CountAsync(string table)
    {
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {table}", fixture.Connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
