namespace MambaMQ.Protocol.Commands.Exchanges;

public sealed class ExchangeDeleteCommand(Guid exchangeId) : ICommand
{
    public FrameType Type => FrameType.ExchangeDeleteCommand;
    public Guid ExchangeId { get; } = exchangeId;
}