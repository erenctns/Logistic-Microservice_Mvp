using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Infrastructure.Persistence.Configurations;

// Eslemeler Domain'e DEGIL buraya yazilir: Order entity'sinde tek bir
// EF attribute'u yok, oyle de kalmali (Domain'de sifir bagimlilik).
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Tablo adi snake_case konvansiyonundan otomatik gelir: "orders".
        builder.HasKey(order => order.Id);

        builder.Property(order => order.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(250);

        // Enum'lari SAYI degil METIN olarak sakliyoruz.
        // Neden? Veritabanina bakan insan "2" yerine "Completed" gorsun ve
        // enum'a yeni bir deger eklendiginde eski satirlarin anlami kaymasin.
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.PackageSize)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.CreatedAt).IsRequired();

        // "Musterinin siparisleri" sorgusu (GET /my-orders) bu index'i kullanacak.
        builder.HasIndex(order => order.CustomerId);

        // BaseEntity'nin domain event listesi veritabanina yazilmaz:
        // o, sureç icinde yasayan bir mekanizma.
        builder.Ignore(order => order.DomainEvents);
    }
}
