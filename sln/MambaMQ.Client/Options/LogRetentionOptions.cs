namespace MambaMQ.Client.Options;

public sealed class LogRetentionOptions
{
    public bool Enabled { get; init; } = false;
    public QueueLogLevel LogLevel { get; init; } = QueueLogLevel.None;
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromHours(24);
}