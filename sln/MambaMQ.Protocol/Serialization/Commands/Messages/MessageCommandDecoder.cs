namespace MambaMQ.Protocol.Serialization.Commands.Messages;

internal static class MessageCommandDecoder
{
    internal static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Publish message command");

        MambaMessage message = MessageDecoder.Decode(buffer[offset..]);

        return new PublishMessageCommand(queueName, message);
    }

    internal static DeleteMessageCommand DecodeDelete(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Delete message command");

        if (buffer.Length - offset < CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");

        Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

        return new DeleteMessageCommand(queueName, messageId);
    }
    
    internal static GetMessagesCommand DecodeGetMessages(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Get messages command");

        if (buffer.Length - offset < sizeof(int) * 2)
            throw new InvalidDataException("Get messages command does not contain pagination parameters.");

        int skip = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, sizeof(int)));
        offset += sizeof(int);

        int take = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, sizeof(int)));
        offset += sizeof(int);

        if (offset != buffer.Length)
            throw new InvalidDataException("Get messages command contains unexpected data.");

        if (skip < 0)
            throw new InvalidDataException("Skip cannot be negative.");

        return take <= 0 
            ? throw new InvalidDataException("Take must be greater than zero.") 
            : new GetMessagesCommand(queueName, skip, take);
    }
    
    internal static DeleteMessagesCommand DecodeDeleteMessages(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Delete messages command");

        if (buffer.Length - offset < sizeof(int))
            throw new InvalidDataException("Delete messages command does not contain message count.");

        int messageCount = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, sizeof(int)));

        offset += sizeof(int);

        if (messageCount <= 0)
            throw new InvalidDataException("Message count must be greater than zero.");

        if (buffer.Length - offset != messageCount * CommandConstants.MessageIdSize)
            throw new InvalidDataException("Delete messages command contains an invalid number of MessageIds.");

        ICollection<Guid> messageIds = new List<Guid>(messageCount);

        for (int i = 0; i < messageCount; i++)
        {
            Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

            messageIds.Add(messageId);
            offset += CommandConstants.MessageIdSize;
        }

        return new DeleteMessagesCommand(queueName, messageIds);
    }
}