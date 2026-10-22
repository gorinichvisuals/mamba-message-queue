namespace MambaMQ.Protocol.Commands.Exchanges;

public sealed class ExchangeGetCommand : ICommand
{
    public FrameType Type => FrameType.ExchangeGetCommand;
}