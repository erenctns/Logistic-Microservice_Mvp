using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace SmartLogistics.TestSupport;

// Testler icin GERCEK bir PostgreSQL container'i kaldirir.
//
// Neden in-memory degil? EF Core'un InMemory saglayicisi bir veritabani DEGILDIR:
// transaction semantigi, unique constraint, JSONB, SQL — hicbiri yok. Outbox
// atomikligini onunla test etmek YANLIS GUVEN verir; tam da dogrulamak
// istedigimiz seyi taklit edemiyor. (ADR-005)
//
// Container ~3-5 saniyede kalkar; bu yuzden test sinifi basina degil,
// PostgresCollection uzerinden TUM siniflarca paylasilir.
public class PostgresFixture : IAsyncLifetime
{
    // Compose'daki ile ayni imaj: testin dogruladigi sey ile calisan sistemin
    // ayni surumde olmasi onemli. (Imaj artik kurucuya veriliyor; parametresiz
    // kurucu + WithImage ikilisi Testcontainers 4.15'te obsolete edildi.)
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("smartlogistics_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    // Rastgele bir host portuna baglanir; port cakismasi imkansiz.
    public string ConnectionString => _container.GetConnectionString();

    public NpgsqlConnection Connection =>
        _connection ?? throw new InvalidOperationException(
            "Fixture baslatilmadi. Test sinifi [Collection(nameof(PostgresCollection))] almali.");

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _connection = new NpgsqlConnection(ConnectionString);
        await _connection.OpenAsync();
    }

    // Testler arasi temizlik: tablolari DELETE ile bosaltir, semayi korur
    // (migration'lari tekrar calistirmaktan cok daha hizli).
    //
    // Respawn olusturuldugu ANDAKI semayi fotograflar. Bu yuzden tembel
    // kuruluyor: ilk cagri, tablolar olustuktan sonra gelmeli.
    public async Task ResetAsync()
    {
        _respawner ??= await Respawner.CreateAsync(Connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });

        await _respawner.ResetAsync(Connection);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        // Container silinir; makinede artik kalmaz.
        await _container.DisposeAsync();
    }
}
