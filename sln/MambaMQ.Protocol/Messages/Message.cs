namespace MambaMQ.Protocol.Messages;

public sealed class Message(ReadOnlyMemory<byte> body)
{
    public Guid MessageId { get; } = Guid.CreateVersion7();
    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;
    public ReadOnlyMemory<byte> Body { get; } = body;

    internal Message(Guid messageId, DateTimeOffset receivedAt, ReadOnlyMemory<byte> body) 
        : this(body)
    {
        MessageId = messageId;
        ReceivedAt = receivedAt;
    }
}