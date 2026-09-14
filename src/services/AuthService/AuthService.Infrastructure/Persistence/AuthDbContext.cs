using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLogistics.AuthService.Infrastructure.Identity;

namespace SmartLogistics.AuthService.Infrastructure.Persistence;

// IdentityDbContext, Identity'nin 7 tablosunu (kullanicilar, roller, claim'ler,
// token'lar...) hazir tanimlar. Biz sadece tip parametrelerini veriyoruz.
public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // NEDEN ELLE ISIMLENDIRIYORUZ?
        // UseSnakeCaseNamingConvention() sutunlari ve index'leri ceviriyor
        // (normalized_name, pk_asp_net_roles) ama TABLO adlarini ceviremiyor:
        // Identity onlari ToTable("AspNetUsers") ile ACIKCA belirliyor ve
        // konvansiyon, acik konfigurasyonu ezmez.
        //
        // Firsattan istifade "asp_net_users" yerine sade isimler veriyoruz.
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
