namespace MambaMQ.LoadBalancer.Models;

public sealed class Subscriber(
    IClientConnection connection,
    int maxMessages,
    int maxBytes,
    TimeSpan maxWaitTime,
    int weight)
{
    public IClientConnection Connection { get; } = connection;
    public int MaxMessages { get; } = maxMessages;
    public int MaxBytes { get; } = maxBytes;
    public TimeSpan MaxWaitTime { get; } = maxWaitTime;
    public int Weight { get; } = weight;
    public int InFlight { get; set; }
    public SemaphoreSlim BatchSignal { get; } = new(0);
}