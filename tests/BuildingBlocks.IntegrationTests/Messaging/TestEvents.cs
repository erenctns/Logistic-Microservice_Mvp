using MassTransit;

namespace SmartLogistics.BuildingBlocks.IntegrationTests.Messaging;

// Test amacli sozlesmeler. Gercek event'ler src/Contracts'ta yasar;
// bunlar sadece altyapiyi dogrulamak icin burada.
public sealed record PingEvent(Guid Id, string Text);

public sealed record BoomEvent(Guid Id);

// Consumer'larin ne aldigini testin gorebilmesi icin paylasilan kayit defteri.
public sealed class MessageCollector
{
    private readonly TaskCompletionSource<PingEvent> _firstConsumer =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<PingEvent> _secondConsumer =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _boomAttempts;

    public Task<PingEvent> FirstConsumerReceived => _firstConsumer.Task;

    public Task<PingEvent> SecondConsumerReceived => _secondConsumer.Task;

    // Kac kez denendi: 1 ilk deneme + 3 retry = 4 olmali.
    public int BoomAttempts => Volatile.Read(ref _boomAttempts);

    public void RecordFirst(PingEvent message) => _firstConsumer.TrySetResult(message);

    public void RecordSecond(PingEvent message) => _secondConsumer.TrySetResult(message);

    public void RecordBoomAttempt() => Interlocked.Increment(ref _boomAttempts);
}

// Kuyruk adi: KebabCaseEndpointNameFormatter "Consumer" ekini atip
// PascalCase'i kebab-case'e cevirir -> "ping"
public sealed class PingConsumer(MessageCollector collector) : IConsumer<PingEvent>
{
    public Task Consume(ConsumeContext<PingEvent> context)
    {
        collector.RecordFirst(context.Message);
        return Task.CompletedTask;
    }
}

// Ayni event'i dinleyen IKINCI consumer -> kuyruk adi "audit-ping".
// Publish fan-out oldugu icin ikisi de kendi kopyasini alir.
public sealed class AuditPingConsumer(MessageCollector collector) : IConsumer<PingEvent>
{
    public Task Consume(ConsumeContext<PingEvent> context)
    {
        collector.RecordSecond(context.Message);
        return Task.CompletedTask;
    }
}

// Her zaman patlayan consumer: retry ve _error kuyrugu davranisini test eder.
// Kuyruk adi "boom", hata kuyrugu "boom_error".
public sealed class BoomConsumer(MessageCollector collector) : IConsumer<BoomEvent>
{
    public Task Consume(ConsumeContext<BoomEvent> context)
    {
        collector.RecordBoomAttempt();

        throw new InvalidOperationException("Bilerek patlatildi: DLQ davranisini dogruluyoruz.");
    }
}
