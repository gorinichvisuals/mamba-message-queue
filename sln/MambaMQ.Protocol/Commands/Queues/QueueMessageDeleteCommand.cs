namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueMessageDeleteCommand(string queueName, Guid messageId) : ICommand
{
    public FrameType Type => FrameType.QueueMessageDeleteCommand;
    public string QueueName { get; } = queueName;
    public Guid MessageId { get; } = messageId;
}