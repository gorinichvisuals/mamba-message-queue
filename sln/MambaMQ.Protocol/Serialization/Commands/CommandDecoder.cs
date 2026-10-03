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
            FrameType.ServiceIdentity => DecodeServiceIdentity(buffer),
            FrameType.CreateExchange => DecodeCreateExchange(buffer),
            FrameType.BindExchange => DecodeBindExchange(buffer),
            FrameType.UnbindExchange => DecodeUnbindExchange(buffer),
            FrameType.PublishToExchange => DecodePublishToExchange(buffer),
            
            _ => throw new InvalidDataException($"Unsupported frame type: {type}.")
        };
    }

    private static CreateQueueCommand DecodeCreateQueue(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Create queue command");

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Create queue command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidateLoadBalancingAlgorithm(buffer, offset);

        LoadBalancingAlgorithm loadBalancingAlgorithm = (LoadBalancingAlgorithm)buffer[offset];

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        ValidatePermissions(buffer, offset, out int permissionsCount);

        offset += CommandConstants.PermissionsCountSize;

        Dictionary<string, QueuePermission> permissions = [];

        for (int i = 0; i < permissionsCount; i++)
        {
            ValidateServiceNameLength(buffer, offset, out int serviceNameLength);

            offset += CommandConstants.ServiceNameLengthSize;

            if (buffer.Length < offset + serviceNameLength + CommandConstants.PermissionSize)
                throw new InvalidDataException("Create queue command does not contain complete permission.");

            string serviceName = Encoding.UTF8.GetString(buffer.Slice(offset, serviceNameLength));

            offset += serviceNameLength;

            QueuePermission permission = (QueuePermission)buffer[offset];

            offset += CommandConstants.PermissionSize;

            permissions.Add(serviceName, permission);
        }

        ValidateMessageRetentionEnabled(buffer, offset);

        bool messageRetentionEnabled = DecodeBoolean(buffer[offset], "Create queue command contains invalid MessageRetentionEnabled value.");

        offset += CommandConstants.MessageRetentionEnabledSize;

        ValidateMessageRetentionPeriod(buffer, offset);

        long messageRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, CommandConstants.MessageRetentionPeriodSize));

        TimeSpan messageRetentionPeriod = TimeSpan.FromTicks(messageRetentionPeriodTicks);

        return new CreateQueueCommand(
            queueName,
            isDurable,
            loadBalancingAlgorithm,
            permissions,
            messageRetentionEnabled,
            messageRetentionPeriod);
    }

    private static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Publish message command");

        MambaMessage mambaMessage = MessageDecoder.Decode(buffer[offset..]);

        return new PublishMessageCommand(queueName, mambaMessage);
    }

    private static SubscribeQueueCommand DecodeSubscribeQueue(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Subscribe queue command");

        return new SubscribeQueueCommand(queueName);
    }

    private static SubscribeQueueWithBatchCommand DecodeSubscribeQueueWithBatch(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Subscribe with batch command");

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

        return new SubscribeQueueWithBatchCommand(queueName, maxMessages, maxBytes, maxWaitTime, weight);
    }

    private static DeleteMessageCommand DecodeDelete(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Delete message command");

        ValidateMessageId(buffer, offset);

        Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

        return new DeleteMessageCommand(queueName, messageId);
    }

    private static AuthenticationCommand DecodeAuthentication(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string username = DecodeString(
            buffer,
            ref offset,
            CommandConstants.UsernameLengthSize,
            "username",
            "Authentication command");

        string password = DecodeString(
            buffer,
            ref offset,
            CommandConstants.PasswordLengthSize,
            "password",
            "Authentication command");

        return new AuthenticationCommand(username, password);
    }

    private static ServiceIdentityCommand DecodeServiceIdentity(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string serviceName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.ServiceNameLengthSize,
            "service name",
            "Service identity command");

        return new ServiceIdentityCommand(serviceName);
    }

    private static CreateExchangeCommand DecodeCreateExchange(
        ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.ExchangeNameLengthSize,
            "exchange name",
            "Create exchange command");

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Create exchange command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidateExchangeType(buffer, offset);

        ExchangeType exchangeType = (ExchangeType)buffer[offset];

        return new CreateExchangeCommand(exchangeName, isDurable, exchangeType);
    }

    private static BindExchangeCommand DecodeBindExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.ExchangeNameLengthSize,
            "exchange name",
            "Bind exchange command");

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Bind exchange command");

        string routingKey = DecodeString(
            buffer,
            ref offset,
            CommandConstants.RoutingKeyLengthSize,
            "routing key",
            "Bind exchange command");

        return new BindExchangeCommand(exchangeName, queueName, routingKey);
    }

    private static UnbindExchangeCommand DecodeUnbindExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.ExchangeNameLengthSize,
            "exchange name",
            "Unbind exchange command");

        string queueName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.QueueNameLengthSize,
            "queue name",
            "Unbind exchange command");

        string routingKey = DecodeString(
            buffer,
            ref offset,
            CommandConstants.RoutingKeyLengthSize,
            "routing key",
            "Unbind exchange command");

        return new UnbindExchangeCommand(exchangeName, queueName, routingKey);
    }
    
    private static PublishToExchangeCommand DecodePublishToExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = DecodeString(
            buffer,
            ref offset,
            CommandConstants.ExchangeNameLengthSize,
            "exchange name",
            "Publish to exchange command");

        string routingKey = DecodeString(
            buffer,
            ref offset,
            CommandConstants.RoutingKeyLengthSize,
            "routing key",
            "Publish to exchange command");

        MambaMessage mambaMessage = MessageDecoder.Decode(buffer[offset..]);

        return new PublishToExchangeCommand(exchangeName, routingKey, mambaMessage);
    }

    private static string DecodeString(
        ReadOnlySpan<byte> buffer,
        ref int offset,
        int lengthSize,
        string fieldName,
        string commandName)
    {
        if (buffer.Length < offset + lengthSize)
            throw new InvalidDataException($"{commandName} does not contain {fieldName} length.");

        int length = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, lengthSize));

        if (length < 0)
            throw new InvalidDataException($"{commandName} contains invalid {fieldName} length.");

        offset += lengthSize;

        if (buffer.Length < offset + length)
            throw new InvalidDataException($"{commandName} does not contain complete {fieldName}.");

        string value = Encoding.UTF8.GetString(buffer.Slice(offset, length));

        offset += length;

        return value;
    }

    private static void ValidateLoadBalancingAlgorithm(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.LoadBalancingAlgorithmSize)
            throw new InvalidDataException("Create queue command does not contain LoadBalancingAlgorithm.");

        byte value = buffer[offset];

        if (!Enum.IsDefined((LoadBalancingAlgorithm)value))
            throw new InvalidDataException("Create queue command contains invalid LoadBalancingAlgorithm value.");
    }
    
    private static void ValidatePermissions(ReadOnlySpan<byte> buffer, int offset, out int permissionsCount)
    {
        if (buffer.Length < offset + CommandConstants.PermissionsCountSize)
            throw new InvalidDataException("Create queue command does not contain permissions count.");

        permissionsCount = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.PermissionsCountSize));

        if (permissionsCount < 0)
            throw new InvalidDataException("Create queue command contains invalid permissions count.");
    }

    private static void ValidateServiceNameLength(ReadOnlySpan<byte> buffer, int offset, out int serviceNameLength)
    {
        if (buffer.Length < offset + CommandConstants.ServiceNameLengthSize)
            throw new InvalidDataException("Create queue command does not contain service name length.");

        serviceNameLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.ServiceNameLengthSize));

        if (serviceNameLength <= 0)
            throw new InvalidDataException("Create queue command contains invalid service name length.");
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

    private static void ValidateMessageId(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");
    }

    private static void ValidateIsDurable(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.IsDurableSize)
            throw new InvalidDataException("Command does not contain IsDurable.");
    }

    private static void ValidateMessageRetentionEnabled(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length <offset + CommandConstants.MessageRetentionEnabledSize)
            throw new InvalidDataException("Command does not contain MessageRetentionEnabled.");
    }

    private static void ValidateMessageRetentionPeriod(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.MessageRetentionPeriodSize)
            throw new InvalidDataException("Command does not contain MessageRetentionPeriod.");
    }

    private static void ValidateExchangeType(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + CommandConstants.ExchangeTypeSize)
            throw new InvalidDataException("Create exchange command does not contain ExchangeType.");

        byte value = buffer[offset];

        if (!Enum.IsDefined((ExchangeType)value))
            throw new InvalidDataException("Create exchange command contains invalid ExchangeType value.");
    }
}