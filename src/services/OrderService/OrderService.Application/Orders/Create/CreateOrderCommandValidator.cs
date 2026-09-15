using FluentValidation;

namespace SmartLogistics.OrderService.Application.Orders.Create;

// Bu kurallar Order.Create() icindekilerle KISMEN ayni. Tekrar gibi gorunur
// ama iki ayri isi yapiyorlar:
//
//   Validator  -> GIRDI dogrulamasi. Amaci kullaniciya anlasilir bir 400
//                 donmek ve handler'i bosuna calistirmamak.
//   Order.Create -> IS KURALI. Amaci gecersiz bir nesnenin HIC olusmamasi;
//                 event handler'i, arka plan isi veya seed gibi baska
//                 yollardan gelen cagrilar da ondan geciyor.
//
// Validator'i silsek sistem yine dogru calisir, sadece hata mesaji kabalasir.
// Order.Create kontrolunu silsek is kurali delinebilir hale gelir.
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();

        RuleFor(command => command.DeliveryAddress)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(command => command.PackageSize).IsInEnum();
    }
}
