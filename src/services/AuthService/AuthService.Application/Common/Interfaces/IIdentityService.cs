using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Application.Common.Interfaces;

// Handler'lara donen kullanici bilgisi. Sifre/hash asla disari cikmaz.
public sealed record UserInfo(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);

// ASP.NET Identity'yi SOYUTLAR. Neden?
// UserManager<ApplicationUser> bir NuGet tipidir ve ApplicationUser
// Infrastructure'da yasar. Handler'lar onu dogrudan kullansaydi Application
// katmani Identity'ye bagimli olurdu — bagimlilik oku disari donerdi.
//
// Yan fayda: handler testlerinde bu interface mock'lanabilir (Step 5f).
public interface IIdentityService
{
    Task<Result<Guid>> RegisterAsync(
        string email, string password, string fullName, string role, CancellationToken cancellationToken);

    Task<Result<UserInfo>> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<Result<UserInfo>> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
