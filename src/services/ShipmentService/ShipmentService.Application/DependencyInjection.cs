using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SmartLogistics.ShipmentService.Application;

// Order ve Auth ile ayni kalip, tek farki ValidationBehavior hattinin
// olmamasi: bu servise disaridan komut govdesi gelmiyor (bkz. csproj notu).
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));

        return services;
    }
}
