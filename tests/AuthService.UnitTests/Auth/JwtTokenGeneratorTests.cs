using System.IdentityModel.Tokens.Jwt;
using AwesomeAssertions;
using SmartLogistics.AuthService.Infrastructure.Authentication;
using SmartLogistics.BuildingBlocks.Infrastructure.Authentication;

namespace SmartLogistics.AuthService.UnitTests.Auth;

// Uretilen token'i COZUP iceriginin dogrulugunu kontrol ediyoruz.
// Imza dogrulamasi burada degil; o ASP.NET'in isi ve integration testte
// gercek HTTP istegi ile dolayli olarak dogrulaniyor.
public class JwtTokenGeneratorTests
{
    private static readonly JwtOptions Options = new()
    {
        // Test icin sabit anahtar; gercek anahtar .env'de ve 32+ karakter.
        Key = "test_key_that_is_long_enough_for_hs256_algorithm",
        Issuer = "smart-logistics-test",
        Audience = "smart-logistics-test-clients",
        ExpiryMinutes = 30,
    };

    private readonly JwtTokenGenerator _generator = new(Options);

    [Fact]
    public void Generate_Always_ProducesThreePartToken()
    {
        var token = _generator.Generate(Guid.NewGuid(), "eren@example.com", "Eren Cetin", ["Customer"]);

        // header.payload.signature
        token.Value.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Generate_Always_PutsUserIdInSubjectClaim()
    {
        var userId = Guid.NewGuid();

        var token = _generator.Generate(userId, "eren@example.com", "Eren Cetin", ["Customer"]);

        Read(token.Value).Subject.Should().Be(userId.ToString());
    }

    [Fact]
    public void Generate_WithMultipleRoles_WritesAllOfThem()
    {
        var token = _generator.Generate(
            Guid.NewGuid(), "admin@example.com", "Admin", ["Admin", "Customer"]);

        var roles = Read(token.Value).Claims
            .Where(claim => claim.Type == JwtClaimNames.Role)
            .Select(claim => claim.Value);

        roles.Should().BeEquivalentTo(["Admin", "Customer"]);
    }

    [Fact]
    public void Generate_Always_UsesConfiguredIssuerAndAudience()
    {
        var token = _generator.Generate(Guid.NewGuid(), "eren@example.com", "Eren", ["Customer"]);

        var parsed = Read(token.Value);

        parsed.Issuer.Should().Be(Options.Issuer);
        parsed.Audiences.Should().Contain(Options.Audience);
    }

    [Fact]
    public void Generate_Always_ExpiresAfterConfiguredMinutes()
    {
        var token = _generator.Generate(Guid.NewGuid(), "eren@example.com", "Eren", ["Customer"]);

        token.ExpiresAtUtc.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(Options.ExpiryMinutes), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Generate_Always_KeepsPasswordOutOfPayload()
    {
        // Payload sifreli DEGIL; oraya gizli bilgi konmadigini dogruluyoruz.
        var token = _generator.Generate(Guid.NewGuid(), "eren@example.com", "Eren", ["Customer"]);

        Read(token.Value).Claims.Select(claim => claim.Type)
            .Should().NotContain(type => type.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
}
