namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandDecoder
{
    public static ICommand Decode(FrameType type, ReadOnlySpan<byte> buffer)
    {
        return type switch
        {
            FrameType.CreateQueue => DecodeCreateQueue(buffer),
            FrameType.PublishMessage => DecodePublish(buffer),
            FrameType.SubscribeQueue => DecodeSubscribeQueue(buffer),
            FrameType.SubscribeQueueWithBatch => DecodeSubscribeQueueWithBatch(buffer),
            FrameType.DeleteMessage => DecodeDelete(buffer),
            FrameType.Authentication => DecodeAuthentication(buffer),

            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }

    private static CreateQueueCommand DecodeCreateQueue(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Create queue command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidateLoadBalancingAlgorithm(buffer, offset);

        LoadBalancingAlgorithm loadBalancingAlgorithm = (LoadBalancingAlgorithm)buffer[offset];

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        ValidateMessageRetentionEnabled(buffer, offset);

        bool messageRetentionEnabled = DecodeBoolean(buffer[offset], "Create queue command contains invalid MessageRetentionEnabled value.");

        offset += CommandConstants.MessageRetentionEnabledSize;

        ValidateMessageRetentionPeriod(buffer, offset);

        long messageRetentionPeriodTicks =
            BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, CommandConstants.MessageRetentionPeriodSize));

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
            loadBalancingAlgorithm,
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

    private static SubscribeQueueCommand DecodeSubscribeQueue(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out _);

        return new SubscribeQueueCommand(queueName);
    }

    private static SubscribeQueueWithBatchCommand DecodeSubscribeQueueWithBatch(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateMaxMessages(buffer, offset);

        int maxMessages = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.MaxMessagesSize));

        offset += CommandConstants.MaxMessagesSize;

        ValidateMaxBytes(buffer, offset);

        int maxBytes = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.MaxBytesSize));

        offset += CommandConstants.MaxBytesSize;

        ValidateMaxWaitTime(buffer, offset);

        long maxWaitTimeTicks = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, CommandConstants.MaxWaitTimeSize));

        TimeSpan maxWaitTime = TimeSpan.FromTicks(maxWaitTimeTicks);

        offset += CommandConstants.MaxWaitTimeSize;

        ValidateWeight(buffer, offset);

        int weight = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.WeightSize));

        return new SubscribeQueueWithBatchCommand(
            queueName,
            maxMessages,
            maxBytes,
            maxWaitTime,
            weight);
    }
    
    private static DeleteMessageCommand DecodeDelete(ReadOnlySpan<byte> buffer)
    {
        string queueName = DecodeQueueName(buffer, out int offset);

        ValidateMessageId(buffer, offset);

        Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

        return new DeleteMessageCommand(queueName, messageId);
    }
    
    private static AuthenticationCommand DecodeAuthentication(ReadOnlySpan<byte> buffer)
    {
        const int lengthSize = sizeof(int);

        if (buffer.Length < lengthSize)
            throw new InvalidDataException("Command does not contain username length.");

        int usernameLength = BinaryPrimitives.ReadInt32BigEndian(buffer[..lengthSize]);

        if (usernameLength <= 0)
            throw new InvalidDataException("Invalid username length.");

        int offset = lengthSize;

        if (buffer.Length < offset + usernameLength + lengthSize)
            throw new InvalidDataException("Command does not contain complete username.");

        string username = Encoding.UTF8.GetString(buffer.Slice(offset, usernameLength));

        offset += usernameLength;

        int passwordLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, lengthSize));

        if (passwordLength <= 0)
            throw new InvalidDataException("Invalid password length.");

        offset += lengthSize;

        if (buffer.Length < offset + passwordLength)
            throw new InvalidDataException("Command does not contain complete password.");

        string password = Encoding.UTF8.GetString(buffer.Slice(offset, passwordLength));

        return new AuthenticationCommand(username, password);
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

    private static void ValidateMaxMessages(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MaxMessagesSize)
            throw new InvalidDataException("Subscribe with batch command does not contain MaxMessages.");
    }

    private static void ValidateMaxBytes(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MaxBytesSize)
            throw new InvalidDataException("Subscribe with batch command does not contain MaxBytes.");
    }

    private static void ValidateMaxWaitTime(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MaxWaitTimeSize)
            throw new InvalidDataException("Subscribe with batch command does not contain MaxWaitTime.");
    }
    
    private static void ValidateWeight(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.WeightSize)
            throw new InvalidDataException("Subscribe with batch command does not contain Weight.");
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
    
    private static void ValidateLoadBalancingAlgorithm(ReadOnlySpan<byte> buffer, int offset)
    {
        if (offset + CommandConstants.LoadBalancingAlgorithmSize > buffer.Length)
            throw new InvalidDataException("Create queue command does not contain LoadBalancingAlgorithm.");

        byte value = buffer[offset];

        if (!Enum.IsDefined((LoadBalancingAlgorithm)value))
            throw new InvalidDataException("Create queue command contains invalid LoadBalancingAlgorithm value.");
    }
}