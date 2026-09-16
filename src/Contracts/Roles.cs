namespace SmartLogistics.Contracts;

// Rol adlari SERVISLER ARASI bir sozlesmedir: Auth Service token'in "role"
// claim'ine bu degerleri yazar, diger bes servis [Authorize(Roles = ...)]
// ile ayni degerleri okur. Tek kaynak burasi olmali.
//
// Onceden AuthService.Domain'deydi; ikinci bir servis ihtiyac duyunca
// ortak sozlesme projesine tasindi. Iki serviste iki ayri
// "Customer" sabiti olsaydi biri degistiginde digeri sessizce bozulurdu.
//
// string sabit: rol adi hem JWT claim'inde hem attribute'ta geciyor,
// enum oralarda kullanilamaz.
public static class Roles
{
    public const string Customer = "Customer";

    public const string Courier = "Courier";

    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Customer, Courier, Admin];
}
