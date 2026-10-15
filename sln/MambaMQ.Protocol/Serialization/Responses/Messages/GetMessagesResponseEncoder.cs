namespace MambaMQ.Protocol.Serialization.Responses.Messages;

public static class GetMessagesResponseEncoder
{
    public static byte[] Encode(GetMessagesResponse response)
    {
        int payloadSize = response.Messages
            .Aggregate(sizeof(int) * 2, (current, message) => checked(current + MessageConstants.HeaderSize + message.Payload.Length));

        byte[] buffer = new byte[payloadSize];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteInt32BigEndian(span, response.TotalCount);

        BinaryPrimitives.WriteInt32BigEndian(span[sizeof(int)..], response.Messages.Count);

        int offset = sizeof(int) * 2;

        foreach (QueueMessageModel message in response.Messages)
        {
            MambaMessage mambaMessage = new(message.MessageId, message.ReceivedAt, message.Payload);
            
            byte[] encodedMessage = MessageEncoder.Encode(mambaMessage);

            encodedMessage.CopyTo(span[offset..]);
            offset += encodedMessage.Length;
        }

        return buffer;
    }
}