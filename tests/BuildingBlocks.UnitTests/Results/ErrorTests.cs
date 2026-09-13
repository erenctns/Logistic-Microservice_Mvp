using AwesomeAssertions;
using SmartLogistics.BuildingBlocks.Domain.Results;

namespace SmartLogistics.BuildingBlocks.UnitTests.Results;

public class ErrorTests
{
    [Fact]
    public void None_Always_HasEmptyCodeAndMessage()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Equality_WhenSameCodeAndMessage_ReturnsTrue()
    {
        // record olmasinin faydasi: referans degil DEGER karsilastirmasi.
        var first = new Error("order.not_found", "Siparis bulunamadi");
        var second = new Error("order.not_found", "Siparis bulunamadi");

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_WhenDifferentCode_ReturnsFalse()
    {
        var first = new Error("order.not_found", "Ayni mesaj");
        var second = new Error("order.cancelled", "Ayni mesaj");

        first.Should().NotBe(second);
    }

    [Fact]
    public void ToString_Always_ReturnsCode()
    {
        // Log'da ve API cevabinda kullanilan sey koddur, mesaj degil.
        new Error("courier.unavailable", "Uygun kurye yok").ToString()
            .Should().Be("courier.unavailable");
    }
}
