using AwesomeAssertions;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.UnitTests.Domain;

// Saf domain testi: veritabani yok, mock yok, container yok.
// CLAUDE.md kurali — Domain testleri ASLA mock kullanmaz.
public class ShipmentTests
{
    private static readonly Guid Order = Guid.NewGuid();
    private static readonly Guid Customer = Guid.NewGuid();

    private static Shipment ValidShipment() =>
        Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy, Istanbul", "Medium").Value;

    [Fact]
    public void Create_WithValidData_StartsAsCreated()
    {
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy, Istanbul", "Medium");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ShipmentStatus.Created);
        result.Value.OrderId.Should().Be(Order);
        result.Value.CustomerId.Should().Be(Customer);
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Always_NormalizesTheTrackingNumber()
    {
        // Musteri kucuk harfle arasa da bulabilsin diye yazma aninda tek sefer normalize ediliyor.
        var result = Shipment.Create(Order, Customer, "  tr-2026-00042  ", "Besiktas", "Small");

        result.Value.TrackingNumber.Should().Be("TR-2026-00042");
    }

    [Fact]
    public void Create_Always_TrimsTheAddress()
    {
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", "   Besiktas   ", "Small");

        result.Value.DeliveryAddress.Should().Be("Besiktas");
    }

    [Fact]
    public void Create_WithoutOrder_Fails()
    {
        var result = Shipment.Create(Guid.Empty, Customer, "TR-2026-00001", "Kadikoy", "Small");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ShipmentErrors.OrderRequired);
    }

    [Fact]
    public void Create_WithoutCustomer_Fails()
    {
        var result = Shipment.Create(Order, Guid.Empty, "TR-2026-00001", "Kadikoy", "Small");

        result.Error.Should().Be(ShipmentErrors.CustomerRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTrackingNumber_Fails(string trackingNumber)
    {
        var result = Shipment.Create(Order, Customer, trackingNumber, "Kadikoy", "Small");

        result.Error.Should().Be(ShipmentErrors.TrackingNumberRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankAddress_Fails(string address)
    {
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", address, "Small");

        result.Error.Should().Be(ShipmentErrors.AddressRequired);
    }

    [Fact]
    public void Create_WithTooLongAddress_Fails()
    {
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", new string('a', 251), "Small");

        result.Error.Should().Be(ShipmentErrors.AddressTooLong);
    }

    [Fact]
    public void Create_WithBlankPackageSize_Fails()
    {
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy", "  ");

        result.Error.Should().Be(ShipmentErrors.PackageSizeRequired);
    }

    [Fact]
    public void Create_WithUnknownPackageSize_Succeeds()
    {
        // Bilerek: bu servis paket boyutunu YORUMLAMIYOR, tasiyor.
        // Order yarin yeni bir boyut eklerse Shipment'in kirilmamasi gerekir.
        var result = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy", "Oversized");

        result.IsSuccess.Should().BeTrue();
        result.Value.PackageSize.Should().Be("Oversized");
    }

    [Fact]
    public void MarkAssigned_WhenCreated_Succeeds()
    {
        var shipment = ValidShipment();

        var result = shipment.MarkAssigned();

        result.IsSuccess.Should().BeTrue();
        shipment.Status.Should().Be(ShipmentStatus.Assigned);
        shipment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAssigned_WhenAlreadyAssigned_Fails()
    {
        var shipment = ValidShipment();
        shipment.MarkAssigned();

        var result = shipment.MarkAssigned();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shipment.invalid_transition");
    }

    [Fact]
    public void MarkInTransit_WhenNoCourierAssigned_Fails()
    {
        // Created -> InTransit atlanamaz: once kurye atanmali.
        var shipment = ValidShipment();

        var result = shipment.MarkInTransit();

        result.IsFailure.Should().BeTrue();
        shipment.Status.Should().Be(ShipmentStatus.Created);
    }

    [Fact]
    public void MarkDelivered_WhenInTransit_Succeeds()
    {
        var shipment = ValidShipment();
        shipment.MarkAssigned();
        shipment.MarkInTransit();

        var result = shipment.MarkDelivered();

        result.IsSuccess.Should().BeTrue();
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
    }

    [Fact]
    public void MarkAssigned_WhenDelivered_Fails()
    {
        // Delivered terminal: teslim edilmis gonderi geri alinamaz.
        var shipment = ValidShipment();
        shipment.MarkAssigned();
        shipment.MarkInTransit();
        shipment.MarkDelivered();

        shipment.MarkAssigned().IsFailure.Should().BeTrue();
        shipment.Status.Should().Be(ShipmentStatus.Delivered);
    }
}
