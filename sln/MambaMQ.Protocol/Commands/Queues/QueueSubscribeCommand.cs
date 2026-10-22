namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueSubscribeCommand(string queueName) : ICommand
{
    public FrameType Type => FrameType.QueueSubscribeCommand;
    public string QueueName { get; } = queueName;
}