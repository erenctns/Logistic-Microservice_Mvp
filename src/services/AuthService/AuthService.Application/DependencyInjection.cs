using Microsoft.Extensions.DependencyInjection;

namespace SmartLogistics.AuthService.Application;

// Application katmaninin DI kayitlari. Program.cs bu metodu cagirir ve
// icinde ne oldugunu BILMEZ — yeni bir bagimlilik eklendiginde Api projesi
// hic degismez. Ayni dosya 6 servisin hepsinde ayni sekilde duracak.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Step 05: MediatR handler'lari, FluentValidation validator'lari,
        //          ValidationBehavior pipeline'i buraya kaydedilecek.
        return services;
    }
}
