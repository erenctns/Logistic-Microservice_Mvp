using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Infrastructure.Persistence;

// Shipment Service'in KENDI veritabani. Order Service'in order_db'sine
// ASLA baglanmaz (mimari kural: database-per-service). Ihtiyaci olan
// siparis bilgisini sorgu ile degil EVENT ile aliyor.
public class ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : DbContext(options)
{
    // Takip numarasinin sayacini veritabani uretir. Neden MAX(id)+1 degil?
    // Iki consumer ayni anda calisirsa ikisi de ayni degeri okur ve ayni
    // numarayi uretir (race condition). Sequence'in nextval'i atomiktir:
    // iki cagri ASLA ayni sayiyi dondurmez.
    public const string TrackingNumberSequence = "shipment_tracking_seq";

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentDbContext).Assembly);

        modelBuilder.HasSequence<long>(TrackingNumberSequence).StartsAt(1).IncrementsBy(1);

        // Bu uc tablo MassTransit'e ait ve BIZIM veritabanimizda yasiyor.
        //
        //   inbox_state    : ISLENEN mesajlarin kimligi -> idempotency
        //   outbox_message : gonderilecek event'in kendisi (ShipmentCreated)
        //   outbox_state   : hangi mesajlarin teslim edildigi
        //
        // Order Service'te inbox_state olusmustu ama KULLANILMIYORDU
        // (o servis yalnizca yayinliyor). Bu serviste ilk kez hem
        // tuketici hem yayinci oldugumuz icin UCU DE calisacak: consumer
        // tek transaction'da inbox damgasini, shipment satirini ve
        // giden event'i birlikte yazacak.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
