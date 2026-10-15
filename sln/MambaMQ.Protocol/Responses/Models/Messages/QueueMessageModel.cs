namespace MambaMQ.Protocol.Responses.Models.Messages;

public sealed class QueueMessageModel
{
    public required Guid MessageId { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public required byte[] Payload { get; init; }
    public int SizeInBytes => Payload.Length;
    public string Content => Encoding.UTF8.GetString(Payload);
}