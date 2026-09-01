namespace MambaMQ.Protocol.Commands;

public sealed class PublishCommand(string queueName, Message message) : ICommand
{
    public FrameType Type => FrameType.Publish;
    public string QueueName { get; } = queueName;
    public Message Message { get; } = message;
}