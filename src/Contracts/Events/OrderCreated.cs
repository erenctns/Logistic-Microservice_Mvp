namespace SmartLogistics.Contracts.Events;

// PROJENIN ILK GERCEK SOZLESMESI.
//
// Order Service yayinlar, Shipment Service dinler. Ikisi de AYNI
// CLR tipini referans eder — MassTransit exchange adini tipin tam adindan
// uretir: "SmartLogistics.Contracts.Events:OrderCreated".
//
// Neden PackageSize burada enum degil string?
// Contracts projesinin sifir bagimliligi var: OrderService.Domain'deki
// enum'u referans edemez (ve etmemeli — sozlesme, yayinlayanin ic
// tiplerine baglanmamali). Consumer kendi tarafinda yorumlar.
//
// Neden metadata (EventId, SentTime) yok?
// MassTransit bunlari kendi zarfinda tasiyor; consumer ConsumeContext'ten
// okur. Ayni bilgiyi iki yerde tutmak tutarsizlik kaynagi olurdu.
//
// ⚠️ Alan silmek veya yeniden adlandirmak BREAKING CHANGE'dir: Shipment
// Service eski alani okumaya devam eder. Yeni alan eklemek guvenlidir.
public sealed record OrderCreated(
    Guid OrderId,
    Guid CustomerId,
    string DeliveryAddress,
    string PackageSize,
    DateTime CreatedAt);
