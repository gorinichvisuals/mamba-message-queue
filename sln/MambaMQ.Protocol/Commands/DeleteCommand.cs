namespace MambaMQ.Protocol.Commands;

public sealed class DeleteCommand(string queueNameName, Guid messageId)  : ICommand
{
    public FrameType Type => FrameType.Delete;
    public string QueueName { get; } = queueNameName;
    public Guid MessageId { get; } = messageId;
}