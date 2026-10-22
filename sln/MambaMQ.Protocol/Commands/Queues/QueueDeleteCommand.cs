namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueDeleteCommand(Guid queueId) : ICommand
{
    public FrameType Type => FrameType.QueueDeleteCommand;
    public Guid QueueId { get; } = queueId;
}