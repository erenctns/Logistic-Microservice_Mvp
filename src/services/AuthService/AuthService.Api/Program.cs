using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SmartLogistics.AuthService.Api.Common;
using SmartLogistics.AuthService.Application;
using SmartLogistics.AuthService.Infrastructure;
using SmartLogistics.AuthService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Composition root: her katman kendi kayitlarini kendi yapar.
// Bu satir Step 05'te 20 servis eklense bile DEGISMEZ.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// .NET 10'un yerlesik OpenAPI destegi; ayri bir paket gerektirmiyor.
builder.Services.AddControllers();

// Hatalar RFC 9457 (ProblemDetails) formatinda donsun.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddOpenApi();

// Sir appsettings'te degil ortam degiskeninde: ConnectionStrings__AuthDb
// (Linux env degiskeninde ":" gecersiz oldugu icin .NET "__" kullanir.)
// Eksikse uygulama ACILMAZ — yarim yapilandirilmis servis, sessizce
// yanlis calisan servisten iyidir.
var connectionString = builder.Configuration.GetConnectionString("AuthDb")
    ?? throw new InvalidOperationException(
        "ConnectionStrings__AuthDb tanimli degil. docker compose .env'den gecirir; " +
        "lokal calistirirken ortam degiskeni olarak ver.");

builder.Services
    .AddHealthChecks()
    // "ready" etiketi: bu kontrol sadece readiness'ta calisacak.
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// Migration'lari acilista uygula.
// DIKKAT: bu Development kolayligidir. Uretimde migration ayri bir adimdir
// (deploy pipeline'i veya init container) — cunku iki replika ayni anda
// acilirsa ikisi birden semayi degistirmeye calisir.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await database.Database.MigrateAsync();

    // Seed migration'dan SONRA: tablolar olmadan satir yazilamaz.
    await AuthDbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Islenmeyen istisnalari ProblemDetails'e cevirir (ValidationException dahil).
app.UseExceptionHandler();

// Sirasi onemli: once kimlik dogrulama (sen kimsin), sonra yetkilendirme
// (bunu yapabilir misin). Ikisi de endpoint eslemesinden ONCE gelir.
app.UseAuthentication();
app.UseAuthorization();

// LIVENESS — "process ayakta mi?" Hicbir bagimliligi kontrol etmez.
// Predicate = _ => false demek: kayitli hicbir health check'i calistirma.
// Neden? Veritabani bir an dustugunde container RESTART EDILMEMELI.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

// READINESS — "bagimliliklar hazir mi?" Postgres'e gercekten baglanir.
// Bu endpoint Unhealthy dondugunde servis restart edilmez, sadece
// trafik almamasi gerekir (gateway/load balancer bunu okur).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapControllers();

app.Run();

// Top-level statement kullanan bir uygulamanin Program sinifi varsayilan
// olarak internal'dir. WebApplicationFactory<Program> ona erisebilsin diye
// public yapiyoruz — integration testlerin uygulamayi ayaga kaldirma yolu bu.
public partial class Program;
