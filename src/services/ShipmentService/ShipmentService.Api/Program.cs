using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SmartLogistics.ShipmentService.Application;
using SmartLogistics.ShipmentService.Infrastructure;
using SmartLogistics.ShipmentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Composition root: her katman kendi kayitlarini kendi yapar.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Hatalar RFC 9457 (ProblemDetails) formatinda donsun.
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

// Sir appsettings'te degil ortam degiskeninde: ConnectionStrings__ShipmentDb
var connectionString = builder.Configuration.GetConnectionString("ShipmentDb")
    ?? throw new InvalidOperationException(
        "ConnectionStrings__ShipmentDb tanimli degil. docker compose .env'den gecirir.");

builder.Services
    .AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// Migration'lari acilista uygula (Development kolayligi; uretimde ayri adim).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ShipmentDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// LIVENESS — process ayakta mi? Hicbir bagimliligi kontrol etmez:
// veritabani dustugunde container RESTART EDILMEMELI.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

// READINESS — bagimliliklar hazir mi? Postgres'e gercekten baglanir.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapControllers();

app.Run();

// Integration testlerin uygulamayi ayaga kaldirabilmesi icin.
public partial class Program;
