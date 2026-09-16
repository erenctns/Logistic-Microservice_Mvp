namespace SmartLogistics.Contracts.Events;

// Shipment Service yayinlar.
//
// Bu event bir ZINCIRIN ikinci halkasi: OrderCreated'i tuketirken doguyor.
// Ikisi de ayni transaction'da yaziliyor (inbox + outbox), yani "gonderi
// olustu ama kimse haberdar olmadi" durumu imkansiz.
//
// MVP kapsaminda bunu dinleyen bir servis YOK — ve bu bilincli:
// yayinci, kimin dinledigini bilmez. Yarin bir teslimat ya da bildirim
// servisi eklenirse Shipment Service'te tek satir degismeden abone olur.
//
// Neden CustomerId var?
// Bugun kimsenin isine yaramiyor. Ama bir bildirim servisi eklenirse
// "kime haber verecegim" sorusunu cevaplamak zorunda kalir. Alani SIMDI
// eklemek bedava; sonradan eklemek, yayinda olan bir sozlesmeyi
// degistirmek demek. Event'ler bir kez yayina girdiginde geriye donuk
// uyumluluk yuku dogurur.
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
