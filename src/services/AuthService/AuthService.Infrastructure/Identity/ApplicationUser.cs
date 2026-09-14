using Microsoft.AspNetCore.Identity;

namespace SmartLogistics.AuthService.Infrastructure.Identity;

// IdentityUser<Guid> hazir olarak sunlari getirir: Id, Email, UserName,
// PasswordHash, SecurityStamp, EmailConfirmed, LockoutEnd, AccessFailedCount...
// Sifreyi biz hicbir zaman gormeyiz; Identity PBKDF2 ile hash'leyip saklar.
//
// Guid secildi: ID'yi veritabanina gitmeden uretebilmek icin (BaseEntity ile ayni gerekce).
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
