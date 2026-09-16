namespace SmartLogistics.OrderService.Domain;

// Siparisin yasam dongusu. Gecisler Order entity'sinde korunuyor;
// bu enum sadece "hangi durumlar var" sorusunu cevaplar.
//
//   Pending ──► Processing ──► Completed
//      │             │
//      └─────────────┴──► Cancelled
//
// Completed ve Cancelled TERMINAL: oradan cikis yok.
public enum OrderStatus
{
    // Siparis olusturuldu, henuz shipment yaratilmadi.
    Pending = 0,

    // Gonderi olustu, teslimat sureci isliyor.
    Processing = 1,

    // Teslim edildi.
    Completed = 2,

    Cancelled = 3,
}
