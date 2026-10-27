namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueGetListCommand : ICommand
{
    public FrameType Type => FrameType.QueueGetListCommand;
}