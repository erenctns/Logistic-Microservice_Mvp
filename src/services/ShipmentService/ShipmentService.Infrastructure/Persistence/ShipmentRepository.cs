using Microsoft.EntityFrameworkCore;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.Infrastructure.Persistence;

// Application'da TANIMLANAN sozlesmenin EF Core ile gerceklestirimi.
public sealed class ShipmentRepository(ShipmentDbContext context) : IShipmentRepository
{
    public void Add(Shipment shipment) => context.Shipments.Add(shipment);

    public async Task<Shipment?> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await context.Shipments
            .AsNoTracking()   // salt okuma: degisiklik takibi gereksiz maliyet
            .FirstOrDefaultAsync(shipment => shipment.OrderId == orderId, cancellationToken);

    public async Task<Shipment?> GetByTrackingNumberAsync(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        // Yazarken buyuk harfe cevirmistik; ararken de ayni normalizasyon
        // uygulanmali, yoksa "tr-2026-00001" hicbir zaman bulunmaz.
        var normalized = trackingNumber.Trim().ToUpperInvariant();

        return await context.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(shipment => shipment.TrackingNumber == normalized, cancellationToken);
    }
}

// Transaction sinirini EF Core'un degisiklik takipcisi temsil ediyor.
public sealed class UnitOfWork(ShipmentDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
