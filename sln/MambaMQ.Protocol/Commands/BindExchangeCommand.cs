namespace MambaMQ.Protocol.Commands;

public sealed class BindExchangeCommand(
    string exchangeName,
    string queueName,
    string routingKey) : ICommand
{
    public FrameType Type => FrameType.BindExchange;

    public string ExchangeName { get; } = exchangeName;
    public string QueueName { get; } = queueName;
    public string RoutingKey { get; } = routingKey;
}