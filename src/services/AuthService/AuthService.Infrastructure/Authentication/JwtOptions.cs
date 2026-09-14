namespace SmartLogistics.AuthService.Infrastructure.Authentication;

// appsettings'te DEGIL, ortam degiskeninde:
//   Jwt__Key, Jwt__Issuer, Jwt__Audience, Jwt__ExpiryMinutes
// compose bunlari .env'den okuyup container'a gecirir.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Imzalama anahtari. HS256 icin en az 256 bit = 32 karakter sart.
    public string Key { get; init; } = string.Empty;

    // Token'i kim uretti / kim tuketecek. Baska bir sistemin token'i
    // kazara kabul edilmesin diye dogrulanir.
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpiryMinutes { get; init; } = 60;
}
