namespace SmartLogistics.ShipmentService.Application.Common.Interfaces;

// "Bana benzersiz bir takip numarasi ver." Application katmani bunun
// NASIL uretildigini bilmiyor — sequence mi, kod mu, harici servis mi.
//
// Clean Architecture'in somut faydasi burada gorunuyor: numaralandirma
// stratejisi yarin degisirse (ornegin sirket geneli bir numarator servisi
// gelirse) degisen tek dosya Infrastructure'daki implementasyon olur.
public interface ITrackingNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken);
}
