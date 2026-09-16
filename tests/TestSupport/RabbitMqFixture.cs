using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace SmartLogistics.TestSupport;

// Testler icin GERCEK bir RabbitMQ container'i kaldirir.
//
// Neden sahte (in-memory) bus degil? MassTransit'in InMemory transport'u
// exchange, kuyruk, retry ve _error kuyrugu gibi RabbitMQ'ya OZGU davranislari
// taklit etmez. Tam da dogrulamak istedigimiz sey bunlar.
//
// Management eklentili imaj seciliyor (:15672): kuyruk sayaclarini HTTP API
// uzerinden okuyabilmek icin — mesajin _error kuyruguna dustugunu boyle kanitliyoruz.
public class RabbitMqFixture : IAsyncLifetime
{
    private const string User = "guest";
    private const string Pass = "guest";
    private const int ManagementPort = 15672;

    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management")
        .WithUsername(User)
        .WithPassword(Pass)
        // Rastgele bir host portuna baglanir; compose'daki 15672 ile cakismaz.
        .WithPortBinding(ManagementPort, assignRandomHostPort: true)
        .Build();

    private HttpClient? _management;

    public string Host => _container.Hostname;

    public ushort Port => _container.GetMappedPublicPort(5672);

    public string Username => User;

    public string Password => Pass;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _management = new HttpClient
        {
            BaseAddress = new Uri($"http://{Host}:{_container.GetMappedPublicPort(ManagementPort)}"),
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{User}:{Pass}"));
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    // Bir kuyruktaki mesaj sayisi. Kuyruk henuz yoksa null doner —
    // "kuyruk olusmadi" ile "kuyruk bos" ayrimini koruyoruz.
    public async Task<int?> GetMessageCountAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var response = await _management!.GetAsync($"/api/queues/%2F/{queueName}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        // "messages" alani her zaman gelmeyebilir: kuyruk yeni olusmussa
        // management eklentisi istatistigi henuz toplamamis olur. O durumda
        // "kuyruk var ama bos" kabul ediyoruz.
        return document.RootElement.TryGetProperty("messages", out var messages)
            ? messages.GetInt32()
            : 0;
    }

    // Testler arasi temizlik. Kuyruklar container ile birlikte YASAR:
    // bir testten artan mesaj, sonraki testin kabinde tuketilir ve o testi
    // kirletir ("neden iki gonderi olustu?"). Veritabanini her testte
    // sifirliyoruz, broker'i da sifirlamak gerekiyor.
    //
    // Kuyruk henuz olusmamissa 404 doner; bu bir hata degil, sessizce gecilir.
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        using var response = await _management!.DeleteAsync(
            $"/api/queues/%2F/{queueName}/contents",
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _management?.Dispose();
        await _container.DisposeAsync();
    }
}
