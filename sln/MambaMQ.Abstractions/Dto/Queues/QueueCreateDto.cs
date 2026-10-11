namespace MambaMQ.Abstractions.Dto.Queues;

public sealed record QueueCreateDto(
    string QueueName,
    bool IsDurable,
    LoadBalancingAlgorithm LoadBalancingAlgorithm,
    Dictionary<string, QueuePermission> Permissions,
    bool MessageRetentionEnabled,
    TimeSpan MessageRetentionPeriod);