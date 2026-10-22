namespace MambaMQ.Protocol.Commands.Exchanges;

public sealed class ExchangeMessagePublishCommand(
    string exchangeName,
    string routingKey,
    MambaMessage message) : ICommand
{
    public FrameType Type => FrameType.ExchangeMessagePublishCommand;

    public string ExchangeName { get; } = exchangeName;
    public string RoutingKey { get; } = routingKey;
    public MambaMessage Message { get; } = message;
}