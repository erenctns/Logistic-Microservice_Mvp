using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SmartLogistics.AuthService.Application;
using SmartLogistics.AuthService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root: her katman kendi kayitlarini kendi yapar.
// Bu satir Step 05'te 20 servis eklense bile DEGISMEZ.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// .NET 10'un yerlesik OpenAPI destegi; ayri bir paket gerektirmiyor.
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.Run();
