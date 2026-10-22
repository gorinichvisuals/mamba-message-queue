namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueGetCommand : ICommand
{
    public FrameType Type => FrameType.QueueGetCommand;
}