using Microsoft.AspNetCore.Identity;
using SmartLogistics.AuthService.Application.Common;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.Infrastructure.Identity;

// IIdentityService'in ASP.NET Identity ile gerceklestirimi.
// Application katmani UserManager'i hic gormez; burada kapali kaliyor.
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IIdentityService
{
    public async Task<Result<Guid>> RegisterAsync(
        string email,
        string password,
        string fullName,
        string role,
        CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<Guid>(AuthErrors.EmailAlreadyExists);
        }

        // Rolu kullaniciyi OLUSTURMADAN once dogrula. Aksi halde kullanici
        // yaratilip rol atamasi patlarsa ortada rolsuz, yarim bir kayit kalir
        // (kismi basarisizlik). Once kontrol, sonra yazma.
        if (!await roleManager.RoleExistsAsync(role))
        {
            return Result.Failure<Guid>(AuthErrors.RoleNotFound(role));
        }

        var user = new ApplicationUser
        {
            // Id'yi biz uretiyoruz: veritabanina gitmeden kimlik belli olsun.
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FullName = fullName,
        };

        // CreateAsync sifreyi PBKDF2 ile hash'leyip saklar; ham sifre
        // bu satirdan sonra hicbir yerde tutulmaz.
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var detail = string.Join(" ", result.Errors.Select(error => error.Description));
            return Result.Failure<Guid>(AuthErrors.RegistrationFailed(detail));
        }

        await userManager.AddToRoleAsync(user, role);

        return user.Id;
    }

    public async Task<Result<UserInfo>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Kullanici yoksa da "InvalidCredentials" donuyoruz: farkli mesaj
        // vermek, saldirgana "bu e-posta kayitli" bilgisini bedava verir.
        if (user is null)
        {
            return Result.Failure<UserInfo>(AuthErrors.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<UserInfo>(AuthErrors.AccountLocked);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Basarisiz deneme sayaci: 5'i gecince hesap gecici kilitlenir.
            await userManager.AccessFailedAsync(user);
            return Result.Failure<UserInfo>(AuthErrors.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await ToUserInfoAsync(user);
    }

    public async Task<Result<UserInfo>> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null
            ? Result.Failure<UserInfo>(AuthErrors.UserNotFound)
            : await ToUserInfoAsync(user);
    }

    private async Task<UserInfo> ToUserInfoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new UserInfo(user.Id, user.Email ?? string.Empty, user.FullName, [.. roles]);
    }
}
