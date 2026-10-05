namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaQueue(
    Guid Id,
    string Name,
    bool IsDurable,
    DateTimeOffset CreatedAt,
    byte LoadBalancingAlgorithm,
    Dictionary<string, byte> Permissions,
    StoredMessageRetentionOptions MessageRetention);

public sealed record StoredMessageRetentionOptions(
    bool RetentionEnabled, 
    TimeSpan RetentionPeriod);