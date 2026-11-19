namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueStatisticsGetCommand(Guid? queueId = null) : ICommand
{
    public Guid? QueueId { get; } = queueId;
    public FrameType Type => FrameType.QueueStatisticsGetCommand;
}