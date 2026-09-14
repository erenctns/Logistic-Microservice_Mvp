using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLogistics.AuthService.IntegrationTests.Fixtures;

// Uygulamanin TAMAMINI bellekte ayaga kaldirir: gercek DI, gercek middleware
// hatti, gercek controller'lar. Fark, TCP portu dinlememesi — istekler
// dogrudan pipeline'a girer, bu yuzden hizli ve port cakismasi olmaz.
//
// Ayarlari override ediyoruz ki test, gelistirme veritabanina degil
// Testcontainers'in kaldirdigi gecici veritabanina yazsin.
public sealed class AuthApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development: uygulama acilista migration + seed calistiriyor.
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:AuthDb", connectionString);

        builder.UseSetting("Jwt:Key", "integration_test_signing_key_at_least_32_chars");
        builder.UseSetting("Jwt:Issuer", "smart-logistics-test");
        builder.UseSetting("Jwt:Audience", "smart-logistics-test-clients");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");

        builder.UseSetting("Seed:AdminPassword", "Admin1234");
        builder.UseSetting("Seed:DefaultPassword", "Demo1234");
    }
}
