using AwesomeAssertions;
using NSubstitute;
using SmartLogistics.AuthService.Application.Auth.Login;
using SmartLogistics.AuthService.Application.Common;
using SmartLogistics.AuthService.Application.Common.Interfaces;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.AuthService.UnitTests.Auth;

// Handler testi = Application katmani. Burada MOCK SERBEST (Domain'de yasak):
// handler'in isi baska servislerle konusmak, o servisleri sahteleyerek
// AKISIN dogrulugunu test ediyoruz — veritabani veya JWT kutuphanesi olmadan.
public class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private LoginCommandHandler CreateHandler() => new(_identityService, _tokenGenerator);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        var userId = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddMinutes(60);

        _identityService
            .ValidateCredentialsAsync("eren@example.com", "Test1234", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserInfo(userId, "eren@example.com", "Eren Cetin", ["Customer"])));

        _tokenGenerator
            .Generate(userId, "eren@example.com", "Eren Cetin", Arg.Any<IEnumerable<string>>())
            .Returns(new AccessToken("uretilmis.jwt.token", expiry));

        var result = await CreateHandler().Handle(
            new LoginCommand("eren@example.com", "Test1234"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("uretilmis.jwt.token");
        result.Value.UserId.Should().Be(userId);
        result.Value.Roles.Should().ContainSingle().Which.Should().Be("Customer");
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_ReturnsFailureAndNeverGeneratesToken()
    {
        _identityService
            .ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserInfo>(AuthErrors.InvalidCredentials));

        var result = await CreateHandler().Handle(
            new LoginCommand("eren@example.com", "YanlisSifre"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthErrors.InvalidCredentials);

        // Kimlik dogrulanmadiysa token URETILMEMELI. Mock'in asil degeri:
        // "olmayan cagriyi" da test edebilmek.
        _tokenGenerator.DidNotReceiveWithAnyArgs().Generate(default, default!, default!, default!);
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_PropagatesLockError()
    {
        _identityService
            .ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserInfo>(AuthErrors.AccountLocked));

        var result = await CreateHandler().Handle(
            new LoginCommand("eren@example.com", "Test1234"), TestContext.Current.CancellationToken);

        result.Error.Code.Should().Be("auth.account_locked");
    }
}
