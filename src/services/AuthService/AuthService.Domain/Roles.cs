namespace SmartLogistics.AuthService.Domain;

// Sistemdeki roller. Identity'nin KENDISI Infrastructure'da yasar (NuGet paketi,
// Domain'de paket yasak) ama "hangi roller vardir" sorusu bir IS KURALIDIR,
// dolayisiyla cevabi burada durur.
//
// string sabit kullaniyoruz cunku rol adi hem JWT claim'inde hem [Authorize]
// attribute'unda gecer; enum orada kullanilamaz.
public static class Roles
{
    public const string Customer = "Customer";

    public const string Courier = "Courier";

    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Customer, Courier, Admin];
}
