namespace SmartLogistics.ShipmentService.Domain;

// Gonderinin yasam dongusu. Gecisler Shipment entity'sinde korunuyor;
// bu enum sadece "hangi durumlar var" sorusunu cevaplar.
//
//   Created ──► Assigned ──► InTransit ──► Delivered
//
// Delivered TERMINAL: oradan cikis yok.
//
// DIKKAT: bu enum OrderStatus'un kopyasi DEGIL. Siparis ile gonderi
// ayri kavramlar, ayri servislerde yasiyorlar ve ayri hizda ilerliyorlar:
// siparis "Processing"ken gonderi hala "Created" olabilir.
public enum ShipmentStatus
{
    // Gonderi olusturuldu, henuz kurye atanmadi.
    Created = 0,

    // Kurye atandi (Step 11'de CourierAssigned event'i ile gelecek).
    Assigned = 1,

    // Kurye yola cikti (Step 12).
    InTransit = 2,

    // Teslim edildi (Step 12).
    Delivered = 3,
}
