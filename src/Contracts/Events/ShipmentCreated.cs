namespace SmartLogistics.Contracts.Events;

// Shipment Service yayinlar, Delivery Service dinleyecek (Step 10).
//
// Bu event bir ZINCIRIN ikinci halkasi: OrderCreated'i tuketirken doguyor.
// Ikisi de ayni transaction'da yaziliyor (inbox + outbox), yani "gonderi
// olustu ama kimse haberdar olmadi" durumu imkansiz.
//
// Neden CustomerId var?
// Bu event'in bugunku tek consumer'i Delivery Service ve onun musteriye
// ihtiyaci yok. Ama Step 13'te Notification Service de dinleyecek ve
// "kime bildirim gonderecegim" sorusunu cevaplamak zorunda kalacak.
// Alani SIMDI eklemek bedava; sonradan eklemek, yayinda olan bir
// sozlesmeyi degistirmek demek. Event'ler bir kez yayina girdiginde
// geriye donuk uyumluluk yuku dogurur.
//
// ⚠️ Alan silmek veya yeniden adlandirmak BREAKING CHANGE'dir.
public sealed record ShipmentCreated(
    Guid ShipmentId,
    Guid OrderId,
    Guid CustomerId,
    string TrackingNumber,
    string DeliveryAddress,
    string PackageSize,
    DateTime CreatedAt);
