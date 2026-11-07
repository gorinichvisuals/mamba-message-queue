namespace MambaMQ.Abstractions.Dto.Queues;

public sealed record QueueUpdateDto(
    Guid QueueId,
    string QueueName,
    LoadBalancingAlgorithm LoadBalancingAlgorithm,
    Dictionary<string, QueuePermission> Permissions,
    TimeSpan MessageRetentionPeriod);
