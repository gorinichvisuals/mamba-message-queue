namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer)
    {
        return type switch
        {
            FrameType.PublishMessage => DecodePublish(buffer),
            FrameType.SubscribeQueue => DecodeSubscribe(buffer),
            FrameType.DeleteMessage => DecodeDelete(buffer),

            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }

    private static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Publish command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidatePersistMessages(buffer, offset);

        bool persistMessages = DecodeBoolean(buffer[offset], "Publish command contains invalid PersistMessages value.");

        offset += CommandConstants.PersistMessagesSize;

        MambaMessage mambaMessage = MessageDecoder.Decode(buffer[offset..]);

        return new PublishMessageCommand(queueName, isDurable, persistMessages, mambaMessage);
    }

    private static SubscribeQueueCommand DecodeSubscribe(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Subscribe command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidatePersistMessages(buffer, offset);

        bool persistMessages = DecodeBoolean(buffer[offset], "Subscribe command contains invalid PersistMessages value.");

        return new SubscribeQueueCommand(queueName, isDurable, persistMessages);
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

    private static bool DecodeBoolean(byte value, string errorMessage)
    {
        if (value is not 0 and not 1)
            throw new InvalidDataException(errorMessage);

        return value is 1;
    }

    private static void ValidateQueueNameLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < CommandConstants.QueueNameLengthSize)
            throw new InvalidDataException("Command does not contain queue name length.");
    }

    private static void ValidateIsDurable(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.IsDurableSize)
            throw new InvalidDataException("Command does not contain IsDurable.");
    }

    private static void ValidatePersistMessages(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.PersistMessagesSize)
            throw new InvalidDataException("Command does not contain PersistMessages.");
    }

    private static void ValidateMessageId(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");
    }
}