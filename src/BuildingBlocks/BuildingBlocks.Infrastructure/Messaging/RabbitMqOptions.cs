namespace SmartLogistics.BuildingBlocks.Infrastructure.Messaging;

// appsettings'te DEGIL, ortam degiskeninde:
//   RabbitMq__Host, RabbitMq__Port, RabbitMq__Username, RabbitMq__Password
// compose bunlari .env'den okuyup container'a gecirir.
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    // Sanal host: ayni broker uzerinde izole edilmis mantiksal bolum.
    // Tek ortamda "/" yeterli; prod ve test ayni broker'i paylassaydi ayrilirdi.
    public string VirtualHost { get; init; } = "/";

    public string Username { get; init; } = "guest";

    public string Password { get; init; } = "guest";
}
