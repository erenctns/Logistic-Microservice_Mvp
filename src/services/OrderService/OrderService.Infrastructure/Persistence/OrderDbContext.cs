using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Infrastructure.Persistence;

// Order Service'in KENDI veritabani. Baska hicbir servis bu database'e
// baglanmaz (mimari kural: database-per-service).
public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);

        // OUTBOX'IN KALBI: bu uc tablo MassTransit'e ait ve BIZIM
        // veritabanimizda yasiyor. Ayni veritabaninda olmalari sart —
        // "siparis satiri" ile "event satiri" ancak o zaman AYNI
        // transaction'a girebilir.
        //
        //   outbox_message : gonderilecek event'in kendisi (JSON)
        //   outbox_state   : hangi mesajlarin teslim edildigi
        //   inbox_state    : ALINAN mesajlarin kimligi -> idempotency (Step 08)
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddInboxStateEntity();
    }
}
