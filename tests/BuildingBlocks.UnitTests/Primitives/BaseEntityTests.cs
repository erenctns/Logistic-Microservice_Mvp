using AwesomeAssertions;
using SmartLogistics.BuildingBlocks.Domain.Primitives;

namespace SmartLogistics.BuildingBlocks.UnitTests.Primitives;

public class BaseEntityTests
{
    [Fact]
    public void Constructor_WithGivenId_KeepsThatId()
    {
        var id = Guid.NewGuid();

        var entity = new TestEntity(id);

        entity.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_Always_SetsCreatedAtToUtcNow()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void DomainEvents_WhenEntityIsNew_IsEmpty()
    {
        new TestEntity(Guid.NewGuid()).DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_WhenBusinessMethodRuns_RecordsTheEvent()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Ship();

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TestShipped>();
    }

    [Fact]
    public void ClearDomainEvents_AfterPublishing_EmptiesTheList()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.Ship();

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkUpdated_WhenEntityChanges_SetsUpdatedAt()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Ship();

        entity.UpdatedAt.Should().NotBeNull();
    }

    // Domain testinde MOCK YOK (mimari kural): BaseEntity soyut oldugu icin
    // testin kendi somut ornegi yeterli.
    private sealed record TestShipped(Guid EntityId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private sealed class TestEntity(Guid id) : BaseEntity(id)
    {
        public void Ship()
        {
            AddDomainEvent(new TestShipped(Id));
            MarkUpdated();
        }
    }
}
