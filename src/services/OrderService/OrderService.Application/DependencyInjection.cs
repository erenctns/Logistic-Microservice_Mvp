using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.OrderService.Application.Common.Behaviors;

namespace SmartLogistics.OrderService.Application;

// Auth Service ile AYNI kalip: bu dosya servisten servise degismiyor,
// sadece assembly farkli.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
