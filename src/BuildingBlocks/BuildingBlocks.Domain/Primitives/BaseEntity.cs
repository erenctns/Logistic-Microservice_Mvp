namespace SmartLogistics.BuildingBlocks.Domain.Primitives;

// Kimligi olan ve zaman icinde degisen her nesnenin ortak iskeleti.
// Siparis #42 dun Pending, bugun Shipped — alanlari degisti ama hala AYNI siparis;
// entity'yi tanimlayan sey budur.
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    // EF Core'un nesneyi veritabanindan kurabilmesi icin.
    protected BaseEntity()
    {
    }

    // Neden int degil Guid? ID'yi veritabanina gitmeden, kod tarafinda uretebilmek icin.
    // Step 07'de siparis satiri ile event satirini AYNI transaction'da yazacagiz;
    // event'in icine ID'yi koyarken INSERT'in donmesini bekleyemeyiz.
    public Guid Id { get; protected init; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; protected set; }

    // Disariya salt-okunur: listeyi sadece entity'nin kendisi degistirir.
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    // Event'ler outbox'a yazildiktan sonra altyapi bunu cagirir.
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}
