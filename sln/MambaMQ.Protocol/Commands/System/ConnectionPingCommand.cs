namespace MambaMQ.Protocol.Commands.System;

public sealed class ConnectionPingCommand : ICommand
{
    public FrameType Type => FrameType.ConnectionPingCommand;
}