using AwesomeAssertions;
using NSubstitute;
using SmartLogistics.BuildingBlocks.Application.Messaging;
using SmartLogistics.Contracts.Events;
using SmartLogistics.ShipmentService.Application.Common.Interfaces;
using SmartLogistics.ShipmentService.Application.Shipments.Create;
using SmartLogistics.ShipmentService.Domain;

namespace SmartLogistics.ShipmentService.UnitTests.Application;

// Use-case testi: veritabani YOK, bagimliliklar taklit ediliyor.
// Burada test edilen sey akisin kendisi — "hangi durumda ne yapiyor".
public class CreateShipmentCommandHandlerTests
{
    private static readonly Guid Order = Guid.NewGuid();
    private static readonly Guid Customer = Guid.NewGuid();

    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly ITrackingNumberGenerator _trackingNumbers = Substitute.For<ITrackingNumberGenerator>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateShipmentCommandHandler Handler() =>
        new(_shipments, _trackingNumbers, _eventBus, _unitOfWork);

    private static CreateShipmentCommand NewCommand() =>
        new(Order, Customer, "Kadikoy, Istanbul", "Medium");

    public CreateShipmentCommandHandlerTests()
    {
        _trackingNumbers.NextAsync(Arg.Any<CancellationToken>()).Returns("TR-2026-00001");
    }

    [Fact]
    public async Task Handle_WhenNoShipmentExists_CreatesOneAndSaves()
    {
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var result = await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _shipments.Received(1).Add(Arg.Is<Shipment>(shipment => shipment.OrderId == Order));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenShipmentIsCreated_PublishesShipmentCreated()
    {
        // Zincirin ikinci halkasi: gonderi olusunca haber veriliyor.
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns((Shipment?)null);
        _trackingNumbers.NextAsync(Arg.Any<CancellationToken>()).Returns("TR-2026-00007");

        await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ShipmentCreated>(published =>
                published.OrderId == Order
                && published.CustomerId == Customer
                && published.TrackingNumber == "TR-2026-00007"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenShipmentAlreadyExists_PublishesNothing()
    {
        // Tekrar eden mesaj ikinci bir ShipmentCreated dogurmamali,
        // yoksa Delivery Service ayni gonderi icin iki kez tetiklenirdi.
        var existing = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy", "Medium").Value;
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns(existing);

        await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<ShipmentCreated>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenShipmentAlreadyExists_DoesNotCreateASecondOne()
    {
        // ⭐ ADIMIN CEKIRDEK KURALI: ayni siparis icin ikinci cagri
        // yeni kayit acmaz. Mesaj iki kez gelirse tek gonderi olusur.
        var existing = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy", "Medium").Value;
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        // Sonuc BASARI ve MEVCUT kaydin id'si. Hata donseydik consumer
        // mesaji islenemedi sayip bosuna retry ederdi.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);

        _shipments.DidNotReceive().Add(Arg.Any<Shipment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenShipmentAlreadyExists_DoesNotBurnATrackingNumber()
    {
        // Takip numarasi uretimi sequence'i ilerletir ve GERI ALINAMAZ.
        // Tekrar eden mesajda numara uretmek, her tekrar icin bir numara
        // yakmak demek olurdu.
        var existing = Shipment.Create(Order, Customer, "TR-2026-00001", "Kadikoy", "Medium").Value;
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns(existing);

        await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        await _trackingNumbers.DidNotReceive().NextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainRulesFail_DoesNotSave()
    {
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var command = NewCommand() with { DeliveryAddress = "   " };

        var result = await Handler().Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ShipmentErrors.AddressRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Always_UsesTheGeneratedTrackingNumber()
    {
        // Takip numarasi komuttan GELMIYOR, servis uretiyor.
        _shipments.GetByOrderAsync(Order, Arg.Any<CancellationToken>()).Returns((Shipment?)null);
        _trackingNumbers.NextAsync(Arg.Any<CancellationToken>()).Returns("TR-2026-00042");

        await Handler().Handle(NewCommand(), TestContext.Current.CancellationToken);

        _shipments.Received(1).Add(
            Arg.Is<Shipment>(shipment => shipment.TrackingNumber == "TR-2026-00042"));
    }
}
