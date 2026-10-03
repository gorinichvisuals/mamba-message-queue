namespace MambaMQ.Persistence.Models;

public sealed record StoredMambaExchange(
    Guid Id,
    string Name,
    bool IsDurable,
    byte Type,
    List<StoredExchangeBinding> Bindings);

public sealed record StoredExchangeBinding(
    string QueueName,
    string RoutingKey);