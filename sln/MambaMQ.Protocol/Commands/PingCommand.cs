namespace MambaMQ.Protocol.Commands;

public sealed class PingCommand : ICommand
{
    public FrameType Type => FrameType.PingCommand;
}
