using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Common;

// Hatalar tek yerde: kod makine icin (log, frontend switch),
// mesaj insan icin. Handler'lar bunlari Result.Failure ile dondurur.
public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists =
        new("auth.email_already_exists", "Bu e-posta adresi zaten kayitli.");

    // Kullanici yok mu, sifre mi yanlis — AYIRMIYORUZ. Ayirmak, saldirgana
    // "bu e-posta sistemde var" bilgisini bedava vermek demektir.
    public static readonly Error InvalidCredentials =
        new("auth.invalid_credentials", "E-posta veya sifre hatali.");

    public static readonly Error AccountLocked =
        new("auth.account_locked", "Hesap gecici olarak kilitlendi. Daha sonra tekrar deneyin.");

    public static readonly Error UserNotFound =
        new("auth.user_not_found", "Kullanici bulunamadi.");

    public static Error RoleNotFound(string role) =>
        new("auth.role_not_found", $"Tanimli olmayan rol: {role}");

    public static Error RegistrationFailed(string detail) =>
        new("auth.registration_failed", detail);
}
