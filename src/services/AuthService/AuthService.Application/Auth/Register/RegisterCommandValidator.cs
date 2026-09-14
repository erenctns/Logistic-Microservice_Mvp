using FluentValidation;
using SmartLogistics.AuthService.Domain;

namespace SmartLogistics.AuthService.Application.Auth.Register;

// Kurallar handler'da degil burada. ValidationBehavior bunu otomatik bulur
// ve komut handler'a ulasmadan calistirir.
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.Password)
            .NotEmpty()
            // Uzunluk, ozel karakter zorunlulugundan daha etkili bir onlem.
            .MinimumLength(8);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Role)
            .Must(role => Roles.All.Contains(role))
            .WithMessage($"Rol su degerlerden biri olmali: {string.Join(", ", Roles.All)}");
    }
}
