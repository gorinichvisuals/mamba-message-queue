namespace MambaMQ.Protocol.Commands;

public sealed class PublishToExchangeCommand(
    string exchangeName,
    string routingKey,
    MambaMessage message) : ICommand
{
    public FrameType Type => FrameType.PublishToExchange;

    public string ExchangeName { get; } = exchangeName;
    public string RoutingKey { get; } = routingKey;
    public MambaMessage Message { get; } = message;
}