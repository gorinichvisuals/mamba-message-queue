namespace MambaMQ.Protocol.Commands.Exchanges;

public sealed class ExchangeUnbindCommand(
    string exchangeName,
    string queueName,
    string routingKey) : ICommand
{
    public FrameType Type => FrameType.ExchangeUnbindCommand;

    public string ExchangeName { get; } = exchangeName;
    public string QueueName { get; } = queueName;
    public string RoutingKey { get; } = routingKey;
}