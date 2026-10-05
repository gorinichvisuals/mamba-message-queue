namespace MambaMQ.Protocol.Commands;

public sealed class DeleteQueueCommand(Guid queueId) : ICommand
{
    public FrameType Type => FrameType.DeleteQueueCommand;
    public Guid QueueId { get; } = queueId;
}