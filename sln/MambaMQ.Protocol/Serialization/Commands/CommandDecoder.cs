namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer, TimeSpan ttl)
    {
        return type switch
        {
            FrameType.PublishMessage => DecodePublish(buffer,ttl),
            FrameType.SubscribeQueue => DecodeSubscribe(buffer),
            FrameType.DeleteMessage => DecodeDelete(buffer),

            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }

    private static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer, TimeSpan ttl)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        MambaMessage mambaMessage = MessageDecoder.Decode(buffer[offset..], ttl);

        return new PublishMessageCommand(queueName, mambaMessage);
    }

    private static SubscribeQueueCommand DecodeSubscribe(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateAutoAcknowledge(buffer, offset);

        byte autoAcknowledgeValue = buffer[offset];

        if (autoAcknowledgeValue is not 0 and not 1)
            throw new InvalidDataException("Subscribe command contains invalid AutoAcknowledge value.");

        bool autoAcknowledge = autoAcknowledgeValue is 1;

        return new SubscribeQueueCommand(queueName, autoAcknowledge);
    }

    private static DeleteMessageCommand DecodeDelete(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateMessageId(buffer, offset);

        Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

        return new DeleteMessageCommand(queueName, messageId);
    }

    private static string DecodeQueueName(ReadOnlySpan<byte> buffer, out int offset)
    {
        ValidateQueueNameLength(buffer);

        int queueNameLength = BinaryPrimitives.ReadInt32BigEndian(buffer[..CommandConstants.QueueNameLengthSize]);

        if (queueNameLength is 0)
            throw new InvalidDataException("Invalid queue name length.");

        offset = CommandConstants.QueueNameLengthSize + queueNameLength;

        return buffer.Length < offset 
            ? throw new InvalidDataException("Command does not contain complete queue name.") 
            : Encoding.UTF8.GetString(buffer.Slice(CommandConstants.QueueNameLengthSize, queueNameLength));
    }
    
    private static void ValidateQueueNameLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < CommandConstants.QueueNameLengthSize)
            throw new InvalidDataException("Command does not contain queue name length.");
    }

    private static void ValidateAutoAcknowledge(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.AutoAcknowledgeSize)
            throw new InvalidDataException("Subscribe command does not contain AutoAcknowledge.");
    }

    private static void ValidateMessageId(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");
    }
}