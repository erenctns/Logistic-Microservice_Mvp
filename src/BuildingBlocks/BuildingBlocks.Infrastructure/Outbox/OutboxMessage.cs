namespace SmartLogistics.BuildingBlocks.Infrastructure.Outbox;

// Dual write probleminin cozumu: event'i broker'a gondermek yerine, once
// is verisiyle AYNI transaction icinde AYNI veritabanina bir satir olarak yaz.
//
//   ┌─ TEK TRANSACTION ──────────────────┐
//   │  INSERT INTO orders          ...   │   ikisi ya birlikte olur
//   │  INSERT INTO outbox_messages ...   │   ya hic olmaz
//   └────────────────────────────────────┘
//                  │ commit
//                  ▼
//        OutboxPublisher (Step 07): ProcessedOn NULL olanlari okur,
//        RabbitMQ'ya basar, damgalar. Broker 10 dakika cokse bile
//        hicbir event kaybolmaz.
//
// Bu adimda sadece SINIF var; tablo ve publisher Step 07'de gelecek.
// Her servis bu tabloyu KENDI veritabaninda tutar.
public class OutboxMessage
{
    // EF Core nesneyi veritabanindan kurarken kullanir.
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    // Event'in adi: "OrderCreated".
    public string Type { get; private set; } = string.Empty;

    // Event'in serialize edilmis hali (JSONB kolonu).
    public string Content { get; private set; } = string.Empty;

    // Olayin gerceklestigi an. Publisher siralamayi buna gore yapar.
    public DateTime OccurredOn { get; private set; }

    // NULL = henuz gonderilmedi. Publisher'in tek filtresi bu.
    public DateTime? ProcessedOn { get; private set; }

    // Son denemede alinan hata mesaji (teshis icin).
    public string? Error { get; private set; }

    // Kac kez denendi. Belirli bir siniri asan mesaj sonsuz donguye girmesin.
    public int RetryCount { get; private set; }

    public static OutboxMessage Create(string type, string content, DateTime occurredOn) =>
        new()
        {
            // Id kod tarafinda uretiliyor: satir veritabanina gitmeden
            // mesajin kimligi belli olsun.
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOn = occurredOn,
        };

    public void MarkProcessed(DateTime processedOn)
    {
        ProcessedOn = processedOn;
        Error = null;
    }

    // ProcessedOn NULL birakilir — bir sonraki turda tekrar denenir.
    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
