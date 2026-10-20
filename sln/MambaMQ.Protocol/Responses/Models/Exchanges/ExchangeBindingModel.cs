namespace MambaMQ.Protocol.Responses.Models.Exchanges;

public sealed class ExchangeBindingModel
{
    public string QueueName { get; init; } = string.Empty;
    public string RoutingKey { get; init; } = string.Empty;
}