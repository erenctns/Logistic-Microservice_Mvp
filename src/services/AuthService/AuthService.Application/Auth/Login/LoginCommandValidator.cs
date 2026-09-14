using FluentValidation;

namespace SmartLogistics.AuthService.Application.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();

        // Girise MinimumLength koymuyoruz: kurallar degisirse eski kullanicilar
        // kendi hesabina giremez hale gelir. Uzunluk kurali KAYIT anina aittir.
        RuleFor(command => command.Password).NotEmpty();
    }
}
