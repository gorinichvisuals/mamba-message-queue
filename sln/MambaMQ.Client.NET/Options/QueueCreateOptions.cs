namespace MambaMQ.Client.NET.Options;

/// <summary>Specifies the settings used to create or update a queue.</summary>
public sealed class QueueCreateOptions
{
    /// <summary>The required queue name.</summary>
    public required string QueueName { get; init; }

    /// <summary>Whether the queue survives server restarts; defaults to <see langword="true"/>.</summary>
    public bool IsDurable { get; init; } = true;

    /// <summary>How messages are distributed among consumers; defaults to round robin.</summary>
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; } = LoadBalancingAlgorithm.RoundRobin;

    /// <summary>The permissions assigned to users for this queue.</summary>
    public Dictionary<string, QueuePermission> Permissions { get; init; } = [];

    /// <summary>Message expiration settings.</summary>
    public MessageRetentionOptions MessageRetention { get; init; } = new();
}