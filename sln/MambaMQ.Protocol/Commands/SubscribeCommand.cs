namespace MambaMQ.Protocol.Commands;

public sealed class SubscribeCommand(string queueName) : ICommand
{
    public FrameType Type => FrameType.Subscribe;
    public string QueueName { get; } = queueName;
}