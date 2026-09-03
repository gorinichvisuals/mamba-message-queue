namespace MambaMQ.Protocol.Messages;

public sealed class MambaMessage(ReadOnlyMemory<byte> body)
{
    public Guid MessageId { get; } = Guid.CreateVersion7();
    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; }
    public ReadOnlyMemory<byte> Body { get; } = body;

    internal MambaMessage(
        Guid messageId, 
        DateTimeOffset receivedAt, 
        ReadOnlyMemory<byte> body, 
        TimeSpan  ttl) 
        : this(body)
    {
        MessageId = messageId;
        ReceivedAt = receivedAt;
        ExpiresAt = receivedAt.Add(ttl);
    }
}