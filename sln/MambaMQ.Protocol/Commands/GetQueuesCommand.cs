namespace MambaMQ.Protocol.Commands;

public sealed class GetQueuesCommand : ICommand
{
    public FrameType Type => FrameType.GetQueuesCommand;
}