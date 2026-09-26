namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaQueue(
    Guid Id,
    string Name,
    bool IsDurable,
    StoredMessageRetentionOptions MessageRetention,
    StoredLogRetentionOptions LogRetention);

public sealed record StoredMessageRetentionOptions(
    bool RetentionEnabled, 
    TimeSpan RetentionPeriod);

public sealed record StoredLogRetentionOptions(
    bool RetentionEnabled,
    LogLevel LogLevel,
    TimeSpan RetentionPeriod);