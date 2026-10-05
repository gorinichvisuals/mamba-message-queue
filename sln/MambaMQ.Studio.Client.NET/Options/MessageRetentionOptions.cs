namespace MambaMQ.Studio.Client.NET.Options;

public sealed class MessageRetentionOptions
{
    public bool Enabled { get; init; } = false;
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromMinutes(10);
}