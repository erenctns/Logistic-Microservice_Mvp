using Microsoft.EntityFrameworkCore;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;

namespace SmartLogistics.ShipmentService.Infrastructure.Persistence;

// "TR-2026-00124" formatinda takip numarasi uretir.
//
// Sayaci PostgreSQL sequence'i veriyor. nextval() atomiktir: es zamanli
// iki cagri ASLA ayni sayiyi almaz. SELECT MAX(...)+1 ise almaz -- iki
// consumer ayni anda okuyup ayni numarayi uretebilirdi.
//
// ⚠️ Bilerek kabul edilen davranis: nextval TRANSACTION'A BAGLI DEGILDIR.
// Geri alinan bir islemin numarasi bosa gider, yani numaralarda BOSLUK
// olur (TR-2026-00007 hic kullanilmamis olabilir). Sequence'in gorevi
// siralilik degil TEKLIK; boslugun bir zarari yok.
public sealed class TrackingNumberGenerator(ShipmentDbContext context) : ITrackingNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        // SqlQueryRaw cunku sequence ADI parametre olamaz (tablo/sequence
        // adlari SQL'de parametrelenemez). Degeri sabit ve bizim
        // kontrolumuzde oldugu icin enjeksiyon riski yok.
        var next = await context.Database
            .SqlQueryRaw<long>(
                $"SELECT nextval('{ShipmentDbContext.TrackingNumberSequence}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"TR-{DateTime.UtcNow:yyyy}-{next:D5}";
    }
}
