namespace MambaMQ.Client.NET.Options;

public sealed class QueueOptions
{
    public required string QueueName { get; init; }
    public bool IsDurable { get; init; } = true;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; } = LoadBalancingAlgorithm.RoundRobin;
    public Dictionary<string, QueuePermission> Permissions { get; } = [];
    
    public MessageRetentionOptions MessageRetention { get; init; } = new();
}