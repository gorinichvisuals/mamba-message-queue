namespace MambaMQ.Client.NET.Options;

/// <summary>Specifies the settings that can be changed on an existing queue.</summary>
public sealed class QueueUpdateOptions
{
    /// <summary>The required queue name.</summary>
    public required string QueueName { get; init; }

    /// <summary>How messages are distributed among consumers.</summary>
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; } = LoadBalancingAlgorithm.RoundRobin;

    /// <summary>The permissions assigned to users for this queue.</summary>
    public Dictionary<string, QueuePermission> Permissions { get; init; } = [];

    /// <summary>The message retention period used for storage cleanup.</summary>
    public required TimeSpan MessageRetentionPeriod { get; init; }
}