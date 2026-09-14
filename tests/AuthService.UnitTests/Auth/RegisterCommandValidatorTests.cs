using AwesomeAssertions;
using SmartLogistics.AuthService.Application.Auth.Register;

namespace SmartLogistics.AuthService.UnitTests.Auth;

// Validator saf bir fonksiyon gibidir: girdi ver, kural sonucunu al.
// Veritabani, mock, container yok — en ucuz test seviyesi.
public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid() =>
        new("eren@example.com", "Test1234", "Eren Cetin", "Customer");

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("bozuk@")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567")]
    public void Validate_WithShortPassword_Fails(string password)
    {
        var result = _validator.Validate(Valid() with { Password = password });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithUnknownRole_Fails()
    {
        // "Yonetici" sistemde tanimli degil; sadece Customer/Courier/Admin var.
        var result = _validator.Validate(Valid() with { Role = "Yonetici" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Role");
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Courier")]
    [InlineData("Admin")]
    public void Validate_WithKnownRole_Passes(string role)
    {
        _validator.Validate(Valid() with { Role = role }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyFullName_Fails()
    {
        var result = _validator.Validate(Valid() with { FullName = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "FullName");
    }
}
