namespace MambaMQ.Client.NET.Options;

/// <summary>Controls how messages are grouped during a batch subscription.</summary>
public sealed class BatchSubscribeOptions
{
    /// <summary>The maximum number of messages in a batch.</summary>
    public int MaxMessages { get; init; }

    /// <summary>The maximum serialized batch size in bytes.</summary>
    public int MaxBytes { get; init; }

    /// <summary>The maximum time to wait while forming a batch.</summary>
    public TimeSpan MaxWaitTime { get; init; }

    /// <summary>The consumer weight used by the server when distributing batches; defaults to 1.</summary>
    public int Weight { get; init; } = 1;
}