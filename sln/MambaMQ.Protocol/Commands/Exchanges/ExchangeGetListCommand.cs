namespace MambaMQ.Protocol.Commands.Exchanges;

public sealed class ExchangeGetListCommand : ICommand
{
    public FrameType Type => FrameType.ExchangeGetListCommand;
}