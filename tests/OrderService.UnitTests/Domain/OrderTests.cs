using AwesomeAssertions;
using SmartLogistics.OrderService.Domain;

namespace SmartLogistics.OrderService.UnitTests.Domain;

// Saf domain testi: veritabani yok, mock yok, container yok.
// CLAUDE.md kurali — Domain testleri ASLA mock kullanmaz.
public class OrderTests
{
    private static readonly Guid Customer = Guid.NewGuid();

    private static Order ValidOrder() =>
        Order.Create(Customer, "Kadikoy, Istanbul", PackageSize.Medium).Value;

    [Fact]
    public void Create_WithValidData_StartsAsPending()
    {
        var result = Order.Create(Customer, "Kadikoy, Istanbul", PackageSize.Medium);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Pending);
        result.Value.CustomerId.Should().Be(Customer);
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Always_TrimsTheAddress()
    {
        var result = Order.Create(Customer, "   Besiktas   ", PackageSize.Small);

        result.Value.DeliveryAddress.Should().Be("Besiktas");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankAddress_Fails(string address)
    {
        var result = Order.Create(Customer, address, PackageSize.Small);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressRequired);
    }

    [Fact]
    public void Create_WithTooLongAddress_Fails()
    {
        var result = Order.Create(Customer, new string('a', 251), PackageSize.Small);

        result.Error.Should().Be(OrderErrors.AddressTooLong);
    }

    [Fact]
    public void Create_WithoutCustomer_Fails()
    {
        var result = Order.Create(Guid.Empty, "Kadikoy", PackageSize.Small);

        result.Error.Should().Be(OrderErrors.CustomerRequired);
    }

    [Fact]
    public void MarkAsProcessing_WhenPending_Succeeds()
    {
        var order = ValidOrder();

        var result = order.MarkAsProcessing();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsProcessing_WhenAlreadyProcessing_Fails()
    {
        var order = ValidOrder();
        order.MarkAsProcessing();

        var result = order.MarkAsProcessing();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("order.invalid_transition");
    }

    [Fact]
    public void Complete_WhenPending_FailsBecauseShipmentNeverStarted()
    {
        // Pending -> Completed atlanamaz: arada Processing olmali.
        var order = ValidOrder();

        var result = order.Complete();

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Complete_WhenProcessing_Succeeds()
    {
        var order = ValidOrder();
        order.MarkAsProcessing();

        var result = order.Complete();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Cancel_WhenPending_Succeeds()
    {
        var order = ValidOrder();

        order.Cancel().IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_Fails()
    {
        // Teslim edilmis siparis iptal edilemez — is kuralinin kendisi.
        var order = ValidOrder();
        order.MarkAsProcessing();
        order.Complete();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Fails()
    {
        var order = ValidOrder();
        order.Cancel();

        order.Cancel().IsFailure.Should().BeTrue();
    }
}
