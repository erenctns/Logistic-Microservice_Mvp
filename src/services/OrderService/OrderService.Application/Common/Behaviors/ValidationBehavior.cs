using FluentValidation;
using MediatR;

namespace SmartLogistics.OrderService.Application.Common.Behaviors;

// Her komut handler'a ulasmadan once bu hattan gecer; gecersizse handler
// hic calismaz. Auth Service'teki dosyanin ayni kopyasi.
//
// NOT: ayni sinif iki serviste tekrar ediyor. BuildingBlocks'a tasinabilirdi
// ama o zaman FluentValidation bagimliligi ortak katmana girerdi. Ucuncu
// serviste de gerekirse tasima karari yeniden degerlendirilecek.
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(result => result.Errors).ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
