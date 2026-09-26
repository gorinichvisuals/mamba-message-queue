namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer)
    {
        return type switch
        {
            FrameType.CreateQueue => DecodeCreateQueue(buffer),
            FrameType.PublishMessage => DecodePublish(buffer),
            FrameType.SubscribeQueue => DecodeSubscribe(buffer),
            FrameType.DeleteMessage => DecodeDelete(buffer),

            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }

    private static CreateQueueCommand DecodeCreateQueue(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Create queue command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidateMessageRetentionEnabled(buffer, offset);

        bool messageRetentionEnabled = DecodeBoolean(buffer[offset], "Create queue command contains invalid MessageRetentionEnabled value.");

        offset += CommandConstants.MessageRetentionEnabledSize;

        ValidateMessageRetentionPeriod(buffer, offset);

        long messageRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, CommandConstants.MessageRetentionPeriodSize));

        TimeSpan messageRetentionPeriod = TimeSpan.FromTicks(messageRetentionPeriodTicks);

        offset += CommandConstants.MessageRetentionPeriodSize;

        ValidateLogsRetentionEnabled(buffer, offset);

        bool logRetentionEnabled = DecodeBoolean(buffer[offset], "Create queue command contains invalid LogRetentionEnabled value.");

        offset += CommandConstants.LogsRetentionEnabledSize;

        ValidateLogLevel(buffer, offset);

        byte logLevel = buffer[offset];

        offset += CommandConstants.LogLevelSize;

        ValidateLogsRetentionPeriod(buffer, offset);

        long logRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, CommandConstants.LogsRetentionPeriodSize));

        TimeSpan logRetentionPeriod = TimeSpan.FromTicks(logRetentionPeriodTicks);

        return new CreateQueueCommand(
            queueName,
            isDurable,
            messageRetentionEnabled,
            messageRetentionPeriod,
            logRetentionEnabled,
            logLevel,
            logRetentionPeriod);
    }

    private static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        MambaMessage mambaMessage = MessageDecoder.Decode(buffer[offset..]);

        return new PublishMessageCommand(queueName, mambaMessage);
    }

    private static SubscribeQueueCommand DecodeSubscribe(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out _);

        return new SubscribeQueueCommand(queueName);
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

    private static void ValidateMessageRetentionEnabled(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageRetentionEnabledSize)
            throw new InvalidDataException("Command does not contain MessageRetentionEnabled.");
    }

    private static void ValidateMessageRetentionPeriod(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageRetentionPeriodSize)
            throw new InvalidDataException("Command does not contain MessageRetentionPeriod.");
    }

    private static void ValidateLogsRetentionEnabled(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.LogsRetentionEnabledSize)
            throw new InvalidDataException("Command does not contain LogRetentionEnabled.");
    }

    private static void ValidateLogsRetentionPeriod(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.LogsRetentionPeriodSize)
            throw new InvalidDataException("Command does not contain LogRetentionPeriod.");
    }

    private static void ValidateMessageId(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");
    }

    private static void ValidateLogLevel(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.LogLevelSize)
            throw new InvalidDataException("Command does not contain LogLevel.");
    }
}