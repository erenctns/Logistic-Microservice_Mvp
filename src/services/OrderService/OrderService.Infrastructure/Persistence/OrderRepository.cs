using Microsoft.EntityFrameworkCore;
using SmartLogistics.OrderService.Application.Common.Interfaces;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.Infrastructure.Persistence;

// Application'da TANIMLANAN sozlesmenin EF Core ile gerceklestirimi.
public sealed class OrderRepository(OrderDbContext context) : IOrderRepository
{
    public void Add(Order order) => context.Orders.Add(order);

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        await context.Orders.FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        await context.Orders
            .AsNoTracking()   // salt okuma: degisiklik takibi gereksiz maliyet
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
}

// Transaction sinirini EF Core'un degisiklik takipcisi temsil ediyor:
// SaveChangesAsync, o ana kadar biriken TUM degisiklikleri (siparis satiri
// + outbox satiri) tek transaction'da yazar.
public sealed class UnitOfWork(OrderDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
