namespace MambaMQ.Protocol.Serialization.Messages;

public static class MessageBatchEncoder
{
    public static byte[] Encode(IReadOnlyCollection<MambaMessage> messages)
    {
        if (messages.Count is 0)
            throw new ArgumentException("Batch must contain at least one message.", nameof(messages));

        int payloadSize = MessageConstants.BatchMessageCountSize;

        foreach (MambaMessage message in messages)
            payloadSize += MessageConstants.HeaderSize + message.Body.Length;

        byte[] buffer = new byte[payloadSize];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteInt32BigEndian(span[..MessageConstants.BatchMessageCountSize], messages.Count);

        int offset = MessageConstants.BatchMessageCountSize;

        foreach (MambaMessage message in messages)
        {
            byte[] encodedMessage = MessageEncoder.Encode(message);

            encodedMessage.CopyTo(span[offset..]);

            offset += encodedMessage.Length;
        }

        return buffer;
    }
}