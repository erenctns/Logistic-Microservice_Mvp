using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Infrastructure.Persistence.Configurations;

// Eslemeler Domain'e DEGIL buraya yazilir: Shipment entity'sinde tek bir
// EF attribute'u yok, oyle de kalmali (Domain'de sifir bagimlilik).
public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        // Tablo adi snake_case konvansiyonundan otomatik gelir: "shipments".
        builder.HasKey(shipment => shipment.Id);

        builder.Property(shipment => shipment.TrackingNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(shipment => shipment.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(250);

        // Paket boyutu bu serviste string (bkz. Shipment.cs). Order'daki
        // enum degerleri en fazla birkac karakter; 20 fazlasiyla yeter.
        builder.Property(shipment => shipment.PackageSize)
            .IsRequired()
            .HasMaxLength(20);

        // Enum'u SAYI degil METIN olarak sakliyoruz: veritabanina bakan
        // insan "2" yerine "InTransit" gorsun diye.
        builder.Property(shipment => shipment.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(shipment => shipment.CreatedAt).IsRequired();

        // ⭐ IDEMPOTENCY'NIN SON SAVUNMA HATTI.
        //
        // Consumer'daki "zaten var mi" kontrolu iyi niyetli bir filtredir
        // ama yarisi kaybedebilir: iki kopya AYNI ANDA calisirsa ikisi de
        // "yok" cevabini alip ikisi de eklemeye kalkar. Bu index o durumda
        // ikinci INSERT'i veritabani seviyesinde reddeder.
        //
        // Kural su: bir yarisi (race condition) yalnizca TEK bir yerde
        // seri hale getirerek cozebilirsin — o yer veritabanidir.
        builder.HasIndex(shipment => shipment.OrderId).IsUnique();

        // Takip numarasi da benzersiz olmali; ayrica arama bu index'i kullanacak.
        builder.HasIndex(shipment => shipment.TrackingNumber).IsUnique();

        // "Musterinin gonderileri" sorgusu icin.
        builder.HasIndex(shipment => shipment.CustomerId);

        // BaseEntity'nin domain event listesi veritabanina yazilmaz.
        builder.Ignore(shipment => shipment.DomainEvents);
    }
}
