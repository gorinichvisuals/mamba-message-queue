namespace MambaMQ.Client.NET.Options;

public sealed class BatchSubscribeOptions
{
    public int MaxMessages { get; init; }
    public int MaxBytes { get; init; }
    public TimeSpan MaxWaitTime { get; init; }
    public int Weight { get; init; } = 1;
}