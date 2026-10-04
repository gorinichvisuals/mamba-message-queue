namespace MambaMQ.Core.Exchange;

internal sealed class MambaExchange(
    Guid id,
    string name,
    bool isDurable,
    ExchangeType type,
    Dictionary<string, ExchangePermission> permissions)
{
    private readonly List<ExchangeBinding> _bindings = [];

    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public bool IsDurable { get; } = isDurable;
    public ExchangeType Type { get; } = type;
    
    public Dictionary<string, ExchangePermission> Permissions { get; } = permissions;

    public IReadOnlyList<ExchangeBinding> Bindings => _bindings;

    public void Bind(string queueName, string routingKey)
    {
        if (_bindings.Any(exchangeBinding => exchangeBinding.QueueName == queueName && exchangeBinding.RoutingKey == routingKey))
            return;

        _bindings.Add(new ExchangeBinding(queueName, routingKey));
    }

    public void Unbind(string queueName, string routingKey)
        => _bindings.RemoveAll(exchangeBinding => exchangeBinding.QueueName == queueName && exchangeBinding.RoutingKey == routingKey);

    public IReadOnlyList<string> ResolveQueues(string routingKey)
    {
        return Type switch
        {
            ExchangeType.Direct => _bindings
                .Where(exchangeBinding => exchangeBinding.RoutingKey == routingKey)
                .Select(exchangeBinding => exchangeBinding.QueueName)
                .Distinct()
                .ToArray(),

            ExchangeType.Fanout => _bindings
                .Select(exchangeBinding => exchangeBinding.QueueName)
                .Distinct()
                .ToArray(),
            
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public bool HasPermission(string serviceName, ExchangePermission permission)
        => Permissions.TryGetValue(serviceName, out ExchangePermission permissions) && permissions.HasFlag(permission);
}