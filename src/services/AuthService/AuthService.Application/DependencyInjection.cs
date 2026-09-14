using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartLogistics.AuthService.Application.Common.Behaviors;

namespace SmartLogistics.AuthService.Application;

// Application katmaninin DI kayitlari. Program.cs bu metodu cagirir ve
// icinde ne oldugunu BILMEZ. Ayni dosya 6 servisin hepsinde ayni sekilde duracak.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Bu assembly'deki tum IRequestHandler'lari tarayip kaydeder.
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));

        // Ayni sekilde tum AbstractValidator'lari bulur.
        services.AddValidatorsFromAssembly(assembly);

        // TEK SATIR: artik her komut dogrulamadan gecer.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
