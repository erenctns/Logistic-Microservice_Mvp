using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Application.Common.Interfaces;

// Handler'lar EF Core'u, DbContext'i veya SQL'i GORMEZ. Sadece bu
// sozlesmeyi bilirler; implementasyon Infrastructure'da.
//
// Yan fayda: handler testlerinde bu interface mock'lanabilir, veritabani
// olmadan use-case akisi test edilir.
public interface IOrderRepository
{
    // Ekleme SENKRON: nesneyi degisiklik takipcisine koyar, veritabanina
    // yazmaz. Yazma ani SaveChangesAsync'te — outbox'in calismasi icin
    // bu ayrim sart.
    void Add(Order order);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}

// Transaction sinirini temsil eder: "buraya kadar biriken her sey
// TEK SEFERDE ve BIRLIKTE yazilsin".
//
// Outbox tam olarak burada devreye giriyor: SaveChangesAsync cagrildiginda
// hem siparis satiri hem de yayinlanan event'in outbox satiri ayni
// transaction icinde commit ediliyor.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
