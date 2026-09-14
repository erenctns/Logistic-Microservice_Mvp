using FluentValidation;
using MediatR;

namespace SmartLogistics.AuthService.Application.Common.Behaviors;

// MediatR'in asil degeri burasi: her komut handler'a ulasmadan ONCE
// bu hattan gecer.
//
//   Command ──► ValidationBehavior ──► Handler
//                 gecersizse handler HIC calismaz
//
// Alternatif, her handler'in ilk 10 satirini dogrulamaya ayirmakti.
// 20 handler = 20 kopya = biri unutuldugunda sessiz hata.
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
        // Bu komut icin validator yazilmamissa dogrudan gec.
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
            // Api katmani bunu yakalayip 400 + ProblemDetails'e cevirecek (5d).
            throw new ValidationException(failures);
        }

        return await next();
    }
}
