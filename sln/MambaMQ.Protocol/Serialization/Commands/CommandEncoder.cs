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
            
            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }

    private static byte[] EncodeCreateQueue(CreateQueueCommand queueCommand)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(queueCommand.QueueName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in queueCommand.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            permissionsSize += CommandConstants.ServiceNameLengthSize + serviceName.Length + CommandConstants.PermissionSize;
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

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        span[offset] = queueCommand.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)queueCommand.LoadBalancingAlgorithm;

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], queueCommand.Permissions.Count);
        
        offset += CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in queueCommand.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], serviceName.Length);

            offset += CommandConstants.ServiceNameLengthSize;

            serviceName.CopyTo(span[offset..]);

            offset += serviceName.Length;

            span[offset] = (byte)permission.Value;

            offset += CommandConstants.PermissionSize;
        }

        span[offset] = queueCommand.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.MessageRetentionEnabledSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queueCommand.MessageRetentionPeriod.Ticks);

        return buffer;
    }

    private static byte[] EncodePublishMessage(PublishMessageCommand messageCommand)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(messageCommand.QueueName);

        byte[] message = MessageEncoder.Encode(messageCommand.MambaMessage);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[offset + queueName.Length + message.Length];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        message.CopyTo(span[offset..]);

        return buffer;
    }

    private static byte[] EncodeSubscribeQueue(SubscribeQueueCommand queueCommand)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(queueCommand.QueueName);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[offset + queueName.Length];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        return buffer;
    }
    
    private static byte[] EncodeSubscribeQueueWithBatch(SubscribeQueueWithBatchCommand command)
    {
        byte[] queueNameBytes = Encoding.UTF8.GetBytes(command.QueueName);

        int payloadSize =
            CommandConstants.QueueNameLengthSize +
            queueNameBytes.Length +
            CommandConstants.MaxMessagesSize +
            CommandConstants.MaxBytesSize +
            CommandConstants.MaxWaitTimeSize +
            CommandConstants.WeightSize;

        byte[] buffer = new byte[payloadSize];
        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, CommandConstants.QueueNameLengthSize), queueNameBytes.Length);

        offset += CommandConstants.QueueNameLengthSize;

        queueNameBytes.CopyTo(span[offset..]);

        offset += queueNameBytes.Length;

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, CommandConstants.MaxMessagesSize), command.MaxMessages);

        offset += CommandConstants.MaxMessagesSize;

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, CommandConstants.MaxBytesSize), command.MaxBytes);

        offset += CommandConstants.MaxBytesSize;

        BinaryPrimitives.WriteInt64BigEndian(span.Slice(offset, CommandConstants.MaxWaitTimeSize), command.MaxWaitTime.Ticks);

        offset += CommandConstants.MaxWaitTimeSize;

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, CommandConstants.WeightSize), command.Weight);

        return buffer;
    }

    private static byte[] EncodeDeleteMessage(DeleteMessageCommand messageCommand)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(messageCommand.QueueName);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[offset + queueName.Length + CommandConstants.MessageIdSize];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        messageCommand.MessageId.TryWriteBytes(span[offset..]);

        return buffer;
    }

    private static byte[] EncodeAuthentication(AuthenticationCommand command)
    {
        byte[] username = Encoding.UTF8.GetBytes(command.UserName);
        byte[] password = Encoding.UTF8.GetBytes(command.Password);

        int offset = 0;

        byte[] buffer = new byte[CommandConstants.UsernameLengthSize + username.Length + CommandConstants.PasswordLengthSize + password.Length];

        Span<byte> span = buffer;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], username.Length);

        offset += CommandConstants.UsernameLengthSize;

        username.CopyTo(span[offset..]);

        offset += username.Length;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], password.Length);

        offset += CommandConstants.PasswordLengthSize;

        password.CopyTo(span[offset..]);

        return buffer;
    }
    
    private static byte[] EncodeServiceIdentity(ServiceIdentityCommand command)
    {
        byte[] serviceName = Encoding.UTF8.GetBytes(command.ServiceName);

        byte[] buffer = new byte[CommandConstants.ServiceNameLengthSize + serviceName.Length];

        Span<byte> span = buffer;

        BinaryPrimitives.WriteInt32BigEndian(span[..CommandConstants.ServiceNameLengthSize], serviceName.Length);

        serviceName.CopyTo(span[CommandConstants.ServiceNameLengthSize..]);

        return buffer;
    }
    
    private static void WriteQueueName(Span<byte> buffer, byte[] queueName)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer[..CommandConstants.QueueNameLengthSize], queueName.Length);

        queueName.CopyTo(buffer[CommandConstants.QueueNameLengthSize..]);
    }
}