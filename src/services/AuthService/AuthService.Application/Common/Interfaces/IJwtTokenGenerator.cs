namespace SmartLogistics.AuthService.Application.Common.Interfaces;

// Uretilen token ve ne zaman gecersizlesecegi.
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

// SOZLESME: "su kullanici icin imzali bir token uret".
// Application katmani JWT'nin NASIL uretildigini bilmez — imza algoritmasi,
// gizli anahtar, kutuphane secimi Infrastructure'in isi.
public interface IJwtTokenGenerator
{
    AccessToken Generate(Guid userId, string email, string fullName, IEnumerable<string> roles);
}
