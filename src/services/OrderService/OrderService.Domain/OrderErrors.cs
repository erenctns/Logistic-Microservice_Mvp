using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.OrderService.Domain;

// Is kurali ihlalleri. Auth Service'te hatalar Application katmanindaydi
// cunku orada kimlik ALTYAPISINA dairdiler (sifre yanlis, hesap kilitli).
// Burada ihlal edilen sey domain'in kendi kurali: bu yuzden Domain'de.
public static class OrderErrors
{
    public static readonly Error CustomerRequired =
        new("order.customer_required", "Siparis bir musteriye ait olmalidir.");

    public static readonly Error AddressRequired =
        new("order.address_required", "Teslimat adresi bos olamaz.");

    public static readonly Error AddressTooLong =
        new("order.address_too_long", "Teslimat adresi en fazla 250 karakter olabilir.");

    public static readonly Error NotFound =
        new("order.not_found", "Siparis bulunamadi.");

    // Gecersiz durum gecisi: mesaja mevcut durumu koyuyoruz ki
    // log'a bakan kisi neyin neden reddedildigini anlasin.
    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        new("order.invalid_transition", $"Siparis {from} durumundan {to} durumuna gecemez.");
}
