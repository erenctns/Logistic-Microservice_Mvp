using AwesomeAssertions;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.BuildingBlocks.UnitTests.Results;

public class ResultTests
{
    private static readonly Error SampleError = new("order.not_found", "Siparis bulunamadi");

    [Fact]
    public void Success_WhenCreated_IsSuccessfulAndCarriesNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WhenCreated_IsFailedAndCarriesTheError()
    {
        var result = Result.Failure(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Constructor_WhenSuccessCarriesError_Throws()
    {
        // Tutarsiz sonuc programci hatasidir; nesne dogarken engellenir.
        Action act = () => _ = new InconsistentResult(isSuccess: true, SampleError);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WhenFailureCarriesNoError_Throws()
    {
        Action act = () => _ = new InconsistentResult(isSuccess: false, Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Value_WhenSuccess_ReturnsTheValue()
    {
        var result = Result.Success("Kadikoy");

        result.Value.Should().Be("Kadikoy");
    }

    [Fact]
    public void Value_WhenFailure_Throws()
    {
        // Sessizce null donmek yerine yuksek sesle patlamasi bilincli bir tercih.
        var result = Result.Failure<string>(SampleError);

        Action act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Theory]
    [InlineData("order.not_found")]
    [InlineData("courier.unavailable")]
    [InlineData("shipment.already_dispatched")]
    public void Failure_WithAnyErrorCode_KeepsCodeReadable(string code)
    {
        var result = Result.Failure<string>(new Error(code, "mesaj"));

        result.Error.Code.Should().Be(code);
    }

    // Result'in kurucusu protected: tutarsiz durumu test edebilmek icin
    // testin kendi alt sinifi gerekiyor.
    private sealed class InconsistentResult(bool isSuccess, Error error)
        : Result(isSuccess, error);
}
