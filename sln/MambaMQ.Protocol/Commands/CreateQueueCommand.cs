namespace MambaMQ.Protocol.Commands;

public sealed class CreateQueueCommand(
    string queueName, 
    bool isDurable, 
    bool messageRetentionEnabled,
    TimeSpan messageRetentionPeriod,
    bool logRetentionEnabled, 
    byte logLevel,
    TimeSpan logRetentionPeriod) : ICommand
{
    public FrameType Type => FrameType.CreateQueue;
    public string QueueName { get; } = queueName;
    public bool IsDurable { get; } = isDurable;
    public bool MessageRetentionEnabled { get; } =  messageRetentionEnabled;
    public TimeSpan MessageRetentionPeriod { get; } = messageRetentionPeriod;
    public bool LogRetentionEnabled { get; } = logRetentionEnabled;
    public byte LogLevel { get; } = logLevel;
    public TimeSpan LogRetentionPeriod { get; } = logRetentionPeriod;
} 