namespace MambaMQ.Client.NET.Options;

/// <summary>Specifies whether messages expire from a queue and the retention duration.</summary>
public sealed class MessageRetentionOptions
{
    /// <summary>Whether message retention cleanup is enabled; defaults to <see langword="false"/>.</summary>
    public bool Enabled { get; init; } = false;

    /// <summary>How long messages are retained when enabled; defaults to 10 minutes.</summary>
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromMinutes(10);
}