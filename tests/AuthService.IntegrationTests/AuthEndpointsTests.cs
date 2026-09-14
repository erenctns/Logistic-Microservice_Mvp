using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using SmartLogistics.AuthService.IntegrationTests.Fixtures;
using SmartLogistics.TestSupport;

namespace SmartLogistics.AuthService.IntegrationTests;

// GERCEK zincir: Postgres container'i + uygulamanin tam pipeline'i.
// Mock yok; Identity gercekten hash'liyor, EF gercekten yaziyor,
// JWT gercekten imzalanip dogrulaniyor.
[Collection(nameof(PostgresCollection))]
public class AuthEndpointsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AuthApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new AuthApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // Her test kendi e-postasini kullanir: testler ayni veritabanini
    // paylastigi icin birbirinin kaydini bozmasin.
    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@example.com";

    private async Task<HttpResponseMessage> RegisterAsync(
        string email, string password = "Test1234", string role = "Customer") =>
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            fullName = "Test Kullanici",
            role,
        }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        var response = await RegisterAsync(UniqueEmail());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithSameEmailTwice_ReturnsConflict()
    {
        var email = UniqueEmail();
        (await RegisterAsync(email)).StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await RegisterAsync(email);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidInput_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "bozuk",
            password = "123",
            fullName = "",
            role = "Yonetici",
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Email").And.Contain("Password").And.Contain("Role");
    }

    [Fact]
    public async Task Login_AfterRegistration_ReturnsUsableToken()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Test1234" }, TestContext.Current.CancellationToken);

        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        payload!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Token.Split('.').Should().HaveCount(3);
        payload.Roles.Should().ContainSingle().Which.Should().Be("Customer");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "YanlisSifre1" }, TestContext.Current.CancellationToken);

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameResponseAsWrongPassword()
    {
        // Iki durum ayirt edilebilseydi, saldirgan hangi e-postalarin
        // kayitli oldugunu tek tek deneyerek ogrenebilirdi.
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = UniqueEmail(), password = "Test1234" }, TestContext.Current.CancellationToken);

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTokenFromLogin_ReturnsCurrentUser()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Test1234" }, TestContext.Current.CancellationToken);
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", payload!.Token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeResponse>(TestContext.Current.CancellationToken);
        me!.Email.Should().Be(email);
        me.Roles.Should().Contain("Customer");
    }

    [Fact]
    public async Task Me_WithTamperedToken_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Test1234" }, TestContext.Current.CancellationToken);
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);

        // Imzanin son karakterini bozuyoruz: token'in degistirilemez
        // oldugunu kanitlayan test.
        var parts = payload!.Token.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{parts[2][..^1]}X";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", tampered);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Seed_OnStartup_CreatesAdminUser()
    {
        // Seed calisti mi? Admin gercekten giris yapabiliyor mu?
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@smartlogistics.local", password = "Admin1234" },
            TestContext.Current.CancellationToken);

        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        payload!.Roles.Should().Contain("Admin");
    }

    private sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, Guid UserId, string Email, string FullName, string[] Roles);

    private sealed record MeResponse(Guid Id, string Email, string FullName, string[] Roles);
}
