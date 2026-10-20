namespace MambaMQ.Protocol.Commands;

public sealed class DeleteExchangeCommand(Guid exchangeId) : ICommand
{
    public FrameType Type => FrameType.DeleteExchangeCommand;
    public Guid ExchangeId { get; } = exchangeId;
}