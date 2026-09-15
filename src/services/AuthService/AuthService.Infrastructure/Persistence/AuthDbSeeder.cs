using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartLogistics.Contracts;
using SmartLogistics.AuthService.Infrastructure.Identity;

namespace SmartLogistics.AuthService.Infrastructure.Persistence;

// Sistemin calisabilmesi icin gereken baslangic verisi: roller ve demo
// kullanicilar. Siparis gibi is verisi seed EDILMEZ — o frontend'den uretilir.
//
// IDEMPOTENT: her acilista tekrar calisir ama kopya uretmez. Container 10 kez
// restart olsa da tablo ayni kalir.
public static class AuthDbSeeder
{
    // SABIT GUID'ler. Neden rastgele degil?
    // Step 09'da Courier Service bu kullanicilara kendi kurye kayitlarini
    // baglayacak. Id her acilista degisirse o baglanti kurulamaz.
    private static readonly Guid AdminId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CustomerId = new("22222222-2222-2222-2222-222222222222");

    private static readonly (Guid Id, string Email, string FullName)[] Couriers =
    [
        (new("c0000001-0000-0000-0000-000000000001"), "ahmet@smartlogistics.local", "Ahmet Yilmaz"),
        (new("c0000002-0000-0000-0000-000000000002"), "mehmet@smartlogistics.local", "Mehmet Demir"),
        (new("c0000003-0000-0000-0000-000000000003"), "ali@smartlogistics.local", "Ali Kaya"),
        (new("c0000004-0000-0000-0000-000000000004"), "ayse@smartlogistics.local", "Ayse Celik"),
        (new("c0000005-0000-0000-0000-000000000005"), "fatma@smartlogistics.local", "Fatma Sahin"),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AuthDbSeeder));

        // Sifreler koda GOMULMEZ; .env -> compose -> ortam degiskeni.
        var adminPassword = Required(configuration, "Seed:AdminPassword");
        var defaultPassword = Required(configuration, "Seed:DefaultPassword");

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
                logger.LogInformation("Rol olusturuldu: {Role}", role);
            }
        }

        await EnsureUserAsync(userManager, logger,
            AdminId, "admin@smartlogistics.local", "Sistem Yoneticisi", Roles.Admin, adminPassword);

        await EnsureUserAsync(userManager, logger,
            CustomerId, "customer@smartlogistics.local", "Demo Musteri", Roles.Customer, defaultPassword);

        foreach (var courier in Couriers)
        {
            await EnsureUserAsync(userManager, logger,
                courier.Id, courier.Email, courier.FullName, Roles.Courier, defaultPassword);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        Guid id,
        string email,
        string fullName,
        string role,
        string password)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            // Zaten var: dokunma. Idempotentligin tamami bu kontrolde.
            return;
        }

        var user = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FullName = fullName,
            // Seed kullanicisi dogrulama akisindan gecmez.
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var detail = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seed kullanicisi olusturulamadi ({email}): {detail}");
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Seed kullanicisi olusturuldu: {Email} ({Role})", email, role);
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"{key.Replace(':', '_')}__ ortam degiskeni tanimli degil (.env dosyasina bak).");
}
