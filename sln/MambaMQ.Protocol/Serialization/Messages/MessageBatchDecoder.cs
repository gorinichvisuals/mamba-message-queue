namespace MambaMQ.Protocol.Serialization.Messages;

public static class MessageBatchDecoder
{
    public static IReadOnlyList<MambaMessage> Decode(ReadOnlySpan<byte> buffer)
    {
        Validate(buffer);

        int messageCount = BinaryPrimitives.ReadInt32BigEndian(
            buffer[..MessageConstants.BatchMessageCountSize]);

        if (messageCount <= 0)
            throw new ArgumentException(
                "Invalid message count.",
                nameof(buffer));

        int offset = MessageConstants.BatchMessageCountSize;

        List<MambaMessage> messages = new(messageCount);

        for (int i = 0; i < messageCount; i++)
        {
            if (buffer.Length < offset + MessageConstants.HeaderSize)
                throw new ArgumentException("Batch does not contain complete message.", nameof(buffer));

            int bodyLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset + MessageConstants.BodyLengthOffset, MessageConstants.BodyLengthSize));

            if (bodyLength < 0)
                throw new ArgumentException("Invalid message payload length.", nameof(buffer));

            int messageLength = MessageConstants.HeaderSize + bodyLength;

            if (buffer.Length < offset + messageLength)
                throw new ArgumentException("Batch does not contain complete message.", nameof(buffer));

            MambaMessage message = MessageDecoder.Decode(buffer.Slice(offset, messageLength));

            messages.Add(message);

            offset += messageLength;
        }

        if (offset != buffer.Length)
            throw new ArgumentException("Batch contains unexpected data.", nameof(buffer));

        return messages;
    }

    private static void Validate(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MessageConstants.BatchMessageCountSize)
            throw new ArgumentException("Batch data is too short.", nameof(buffer));
    }
}