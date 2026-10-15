namespace MambaMQ.Protocol.Serialization.Responses.Messages;

public static class GetMessagesResponseDecoder
{
    public static GetMessagesResponse Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < sizeof(int) * 2)
            throw new InvalidDataException("Get messages response is too short.");

        int offset = 0;

        int totalCount = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, sizeof(int)));

        offset += sizeof(int);

        int messageCount = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, sizeof(int)));

        offset += sizeof(int);

        if (totalCount < 0)
            throw new InvalidDataException("Total message count cannot be negative.");

        if (messageCount < 0 || messageCount > totalCount)
            throw new InvalidDataException("Invalid message count.");

        List<QueueMessageModel> messages = new(messageCount);

        for (int i = 0; i < messageCount; i++)
        {
            if (buffer.Length - offset < MessageConstants.HeaderSize)
                throw new InvalidDataException("Response does not contain a complete message header.");

            int bodyLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset + MessageConstants.BodyLengthOffset, MessageConstants.BodyLengthSize));

            if (bodyLength < 0)
                throw new InvalidDataException("Message payload length cannot be negative.");

            int messageLength = checked(
                MessageConstants.HeaderSize + bodyLength);

            if (buffer.Length - offset < messageLength)
                throw new InvalidDataException("Response does not contain a complete message.");

            MambaMessage message = MessageDecoder.Decode(buffer.Slice(offset, messageLength));

            messages.Add(new QueueMessageModel
            {
                MessageId = message.MessageId,
                ReceivedAt = message.ReceivedAt,
                Payload = message.Body.ToArray()
            });

            offset += messageLength;
        }

        if (offset != buffer.Length)
            throw new InvalidDataException("Get messages response contains unexpected data.");

        return new GetMessagesResponse
        {
            TotalCount = totalCount,
            Messages = messages
        };
    }
}