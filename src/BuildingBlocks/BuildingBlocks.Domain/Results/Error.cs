namespace SmartLogistics.BuildingBlocks.Domain.Results;

// Bir basarisizligin makine-okunur kodu ve insan-okunur mesaji.
// Kod API cevabinda/logda kullanilir ("order.not_found"), mesaj kullaniciya gosterilir.
// record oldugu icin iki Error ayni degerleri tasiyorsa esittir.
public sealed record Error(string Code, string Message)
{
    // Hata yoklugu. Basarili sonuclar bunu tasir.
    public static readonly Error None = new(string.Empty, string.Empty);

    public override string ToString() => Code;
}
