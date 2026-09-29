namespace MambaMQ.Client.Options;

public sealed class QueueOptions
{
    public required string QueueName { get; init; }
    public bool IsDurable { get; init; } = true;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; } = LoadBalancingAlgorithm.RoundRobin;
    
    public MessageRetentionOptions MessageRetention { get; init; } = new();
    public LogRetentionOptions LogRetention { get; init; } = new();
}