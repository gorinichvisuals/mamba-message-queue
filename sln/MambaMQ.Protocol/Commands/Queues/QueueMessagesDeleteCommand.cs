namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueMessagesDeleteCommand(string queueName, ICollection<Guid> messageIds) : ICommand
{
    public FrameType Type => FrameType.QueueMessagesDeleteCommand;
    public string QueueName { get; } = queueName;
    public ICollection<Guid> MessageIds { get; } = messageIds;
}