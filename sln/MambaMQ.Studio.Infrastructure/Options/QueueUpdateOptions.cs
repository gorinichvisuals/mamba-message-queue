namespace MambaMQ.Studio.Infrastructure.Options;

public sealed class QueueUpdateOptions
{
    public required string QueueName { get; init; }
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; } = LoadBalancingAlgorithm.RoundRobin;
    public Dictionary<string, QueuePermission> Permissions { get; init; } = [];
    public required TimeSpan MessageRetentionPeriod { get; init; }
}
