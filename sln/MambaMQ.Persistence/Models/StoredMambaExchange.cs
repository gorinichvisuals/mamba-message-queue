namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaExchange(
    Guid Id,
    string Name,
    bool IsDurable,
    DateTimeOffset CreatedAt,
    byte Type,
    Dictionary<string, byte> Permissions,
    List<StoredExchangeBinding> Bindings);

public sealed record StoredExchangeBinding(
    string QueueName,
    string RoutingKey);
