namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandEncoder
{
    public static byte[] Encode(ICommand command)
    {
        return command.Type switch
        {
            FrameType.CreateQueue => EncodeCreateQueue((CreateQueueCommand)command),
            FrameType.PublishMessage => EncodePublishMessage((PublishMessageCommand)command),
            FrameType.SubscribeQueue => EncodeSubscribeQueue((SubscribeQueueCommand)command),
            FrameType.SubscribeQueueWithBatch => EncodeSubscribeQueueWithBatch((SubscribeQueueWithBatchCommand)command),
            FrameType.DeleteMessage => EncodeDeleteMessage((DeleteMessageCommand)command),
            FrameType.Authentication => EncodeAuthentication((AuthenticationCommand)command),
            FrameType.ServiceIdentity => EncodeServiceIdentity((ServiceIdentityCommand)command),
            FrameType.CreateExchange => EncodeCreateExchange((CreateExchangeCommand)command),
            FrameType.BindExchange => EncodeBindExchange((BindExchangeCommand)command),
            FrameType.UnbindExchange => EncodeUnbindExchange((UnbindExchangeCommand)command),
            FrameType.PublishToExchange => EncodePublishToExchange((PublishToExchangeCommand)command),
            FrameType.GetQueuesCommand => EncodeGetQueues(),
            FrameType.DeleteQueueCommand => EncodeDeleteQueue((DeleteQueueCommand)command),
            FrameType.UpdateQueueCommand => EncodeUpdateQueue((UpdateQueueCommand)command),

            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }

    private static byte[] EncodeCreateQueue(CreateQueueCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            permissionsSize +=
                CommandConstants.ServiceNameLengthSize +
                serviceName.Length +
                CommandConstants.PermissionSize;
        }

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[
            offset +
            queueName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.LoadBalancingAlgorithmSize +
            permissionsSize +
            CommandConstants.MessageRetentionEnabledSize +
            CommandConstants.MessageRetentionPeriodSize];

        Span<byte> span = buffer;

        offset = WriteString(span, offset - CommandConstants.QueueNameLengthSize, command.QueueName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.LoadBalancingAlgorithm;

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], serviceName.Length);

            offset += CommandConstants.ServiceNameLengthSize;

            serviceName.CopyTo(span[offset..]);

            offset += serviceName.Length;

            span[offset] = (byte)permission.Value;

            offset += CommandConstants.PermissionSize;
        }

        span[offset] = command.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.MessageRetentionEnabledSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], command.MessageRetentionPeriod.Ticks);

        return buffer;
    }

    private static byte[] EncodePublishMessage(PublishMessageCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] message = MessageEncoder.Encode(command.MambaMessage);

        int payloadSize =
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            message.Length;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.QueueName);

        message.CopyTo(span[offset..]);

        return buffer;
    }

    private static byte[] EncodeSubscribeQueue(SubscribeQueueCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        byte[] buffer = new byte[CommandConstants.QueueNameLengthSize + queueName.Length];

        WriteString(buffer, 0, command.QueueName);

        return buffer;
    }

    private static byte[] EncodeSubscribeQueueWithBatch(SubscribeQueueWithBatchCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int payloadSize =
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.MaxMessagesSize +
            CommandConstants.MaxBytesSize +
            CommandConstants.MaxWaitTimeSize +
            CommandConstants.WeightSize;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.QueueName);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.MaxMessages);

        offset += CommandConstants.MaxMessagesSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.MaxBytes);

        offset += CommandConstants.MaxBytesSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], command.MaxWaitTime.Ticks);

        offset += CommandConstants.MaxWaitTimeSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Weight);

        return buffer;
    }

    private static byte[] EncodeDeleteMessage(DeleteMessageCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        byte[] buffer = new byte[
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.MessageIdSize];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.QueueName);

        command.MessageId.TryWriteBytes(span[offset..]);

        return buffer;
    }

    private static byte[] EncodeAuthentication(AuthenticationCommand command)
    {
        byte[] username = Encoding.UTF8.GetBytes(command.UserName);
        byte[] password = Encoding.UTF8.GetBytes(command.Password);

        byte[] buffer = new byte[
            sizeof(byte) +
            CommandConstants.UsernameLengthSize +
            username.Length +
            CommandConstants.PasswordLengthSize +
            password.Length];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = (byte)command.ClientType;

        offset = WriteString(span, offset, command.UserName);
        WriteString(span, offset, command.Password);

        return buffer;
    }
    private static byte[] EncodeServiceIdentity(ServiceIdentityCommand command)
    {
        byte[] serviceName = Encoding.UTF8.GetBytes(command.ServiceName);

        byte[] buffer = new byte[CommandConstants.ServiceNameLengthSize + serviceName.Length];

        WriteString(buffer, 0, command.ServiceName);

        return buffer;
    }

    private static byte[] EncodeCreateExchange(CreateExchangeCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach ((string serviceName, ExchangePermission _) in command.Permissions)
        {
            byte[] serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);

            permissionsSize += CommandConstants.ServiceNameLengthSize + serviceNameBytes.Length + CommandConstants.PermissionSize;
        }

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.ExchangeTypeSize +
            permissionsSize];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.ExchangeName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.ExchangeType;

        offset += CommandConstants.ExchangeTypeSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach ((string serviceName, ExchangePermission permission) in command.Permissions)
        {
            offset = WriteString(span, offset, serviceName);

            span[offset] = (byte)permission;

            offset += CommandConstants.PermissionSize;
        }

        return buffer;
    }

    private static byte[] EncodeBindExchange(BindExchangeCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] routingKey = Encoding.UTF8.GetBytes(command.RoutingKey);

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.RoutingKeyLengthSize +
            routingKey.Length];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.ExchangeName);

        offset = WriteString(span, offset, command.QueueName);

        WriteString(span, offset, command.RoutingKey);

        return buffer;
    }

    private static byte[] EncodeUnbindExchange(UnbindExchangeCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] routingKey = Encoding.UTF8.GetBytes(command.RoutingKey);

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.RoutingKeyLengthSize +
            routingKey.Length];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.ExchangeName);

        offset = WriteString(span, offset, command.QueueName);

        WriteString(span, offset, command.RoutingKey);

        return buffer;
    }
    
    private static byte[] EncodePublishToExchange(PublishToExchangeCommand command)
    {
        byte[] message = MessageEncoder.Encode(command.Message);

        int payloadSize =
            CommandConstants.ExchangeNameLengthSize +
            Encoding.UTF8.GetByteCount(command.ExchangeName) +
            CommandConstants.RoutingKeyLengthSize +
            Encoding.UTF8.GetByteCount(command.RoutingKey) +
            message.Length;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = WriteString(span, 0, command.ExchangeName);

        offset = WriteString(span, offset, command.RoutingKey);

        message.CopyTo(span[offset..]);

        return buffer;
    }
    
    private static byte[] EncodeGetQueues()
    {
        return [];
    }
    
    private static byte[] EncodeDeleteQueue(DeleteQueueCommand command)
    {
        byte[] buffer = new byte[16];

        command.QueueId.TryWriteBytes(buffer);

        return buffer;
    }
    
    private static byte[] EncodeUpdateQueue(UpdateQueueCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            permissionsSize +=
                CommandConstants.ServiceNameLengthSize +
                serviceName.Length +
                CommandConstants.PermissionSize;
        }

        int payloadSize =
            CommandConstants.QueueIdSize +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.LoadBalancingAlgorithmSize +
            permissionsSize +
            CommandConstants.MessageRetentionEnabledSize +
            CommandConstants.MessageRetentionPeriodSize;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = 0;

        command.QueueId.TryWriteBytes(span[offset..]);

        offset += CommandConstants.QueueIdSize;

        offset = WriteString(span, offset, command.QueueName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.LoadBalancingAlgorithm;

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        BinaryPrimitives.WriteInt32BigEndian(
            span[offset..],
            command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            offset = WriteString(
                span,
                offset,
                permission.Key);

            span[offset] = (byte)permission.Value;

            offset += CommandConstants.PermissionSize;
        }

        span[offset] = command.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.MessageRetentionEnabledSize;

        BinaryPrimitives.WriteInt64BigEndian(
            span[offset..],
            command.MessageRetentionPeriod.Ticks);

        return buffer;
    }
    
    private static int WriteString(Span<byte> buffer, int offset, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        BinaryPrimitives.WriteInt32BigEndian(buffer[offset..], bytes.Length);

        offset += sizeof(int);

        bytes.CopyTo(buffer[offset..]);

        return offset + bytes.Length;
    }
}