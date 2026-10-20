namespace MambaMQ.Protocol.Commands;

public sealed class GetExchangesCommand : ICommand
{
    public FrameType Type => FrameType.GetExchangesCommand;
}