namespace SmartLogistics.BuildingBlocks.Infrastructure.Authentication;

// appsettings'te DEGIL, ortam degiskeninde:
//   Jwt__Key, Jwt__Issuer, Jwt__Audience, Jwt__ExpiryMinutes
//
// Onceden AuthService'e ozeldi; diger servisler de token DOGRULAMAK zorunda
// oldugu icin ortak katmana tasindi. Token URETIMI hala Auth'a ozel.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Imzalama anahtari. HS256 icin en az 256 bit = 32 karakter sart.
    // Tum servisler AYNI anahtari bilir: uretilen token'i digerleri dogrulayabilsin diye.
    public string Key { get; init; } = string.Empty;

    // Token'i kim uretti / kim tuketecek. Baska bir sistemin token'i
    // kazara kabul edilmesin diye dogrulanir.
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    // Sadece token URETEN servis (Auth) kullanir.
    public int ExpiryMinutes { get; init; } = 60;
}

// Claim adlari tek yerde: uretici ile dogrulayici ayni adi kullanmak zorunda.
public static class JwtClaimNames
{
    // ClaimTypes.Role uzun bir schemas.microsoft.com URI'si uretir;
    // kisa ad token payload'ini okunabilir tutar.
    public const string Role = "role";

    public const string Subject = "sub";
}
