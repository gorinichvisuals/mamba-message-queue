namespace MambaMQ.Client.Options;

public sealed class QueueOptions
{
    public required string QueueName { get; init; }
    public bool IsDurable { get; init; } = true;
    public MessageRetentionOptions MessageRetention { get; init; } = new();
    public LogRetentionOptions LogRetention { get; init; } = new();
}

public sealed class MessageRetentionOptions
{
    public bool Enabled { get; init; }
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromMinutes(10);
}

public sealed class LogRetentionOptions
{
    public bool Enabled { get; init; } = true;
    public QueueLogLevel LogLevel { get; init; } = QueueLogLevel.All;
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromHours(24);
}