namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueRatesGetCommand(Guid queueId) : ICommand
{
    public Guid QueueId { get; } = queueId;
    public FrameType Type => FrameType.QueueRatesGetCommand;
}