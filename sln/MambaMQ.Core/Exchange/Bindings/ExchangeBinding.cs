namespace MambaMQ.Core.Exchange.Bindings;

internal sealed record ExchangeBinding(
    string QueueName,
    string RoutingKey);