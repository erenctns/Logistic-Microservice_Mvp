using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Application.Common.Interfaces;

// Handler'lar EF Core'u, DbContext'i veya SQL'i GORMEZ. Sadece bu
// sozlesmeyi bilirler; implementasyon Infrastructure'da.
public interface IShipmentRepository
{
    // Ekleme SENKRON: nesneyi degisiklik takipcisine koyar, veritabanina
    // yazmaz. Yazma ani SaveChangesAsync'te — outbox'in calismasi icin
    // bu ayrim sart.
    void Add(Shipment shipment);

    // IDEMPOTENCY'nin is katmanindaki ayagi: "bu siparisin gonderisi
    // zaten var mi?" Ayni soruyu veritabani da UNIQUE index ile soruyor;
    // bu metot sorunun nazik hali, index ise son sozu.
    Task<Shipment?> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken);
}

// Transaction sinirini temsil eder: "buraya kadar biriken her sey
// TEK SEFERDE ve BIRLIKTE yazilsin".
//
// Bu serviste o "her sey" UC satir olacak: inbox_state (mesaj islendi
// damgasi) + shipments (is verisi) + outbox_message (yayinlanan event).
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
