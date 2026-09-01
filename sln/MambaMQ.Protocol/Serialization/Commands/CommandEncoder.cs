namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandEncoder
{
    public static byte[] Encode(ICommand command)
    {
        return command.Type switch
        {
            FrameType.Publish => EncodePublish((PublishCommand)command),
            FrameType.Subscribe => EncodeSubscribe((SubscribeCommand)command),
            FrameType.Delete => EncodeDelete((DeleteCommand)command),

            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }
    
    private static byte[] EncodePublish(PublishCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] message = MessageEncoder.Encode(command.Message);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[ offset + queueName.Length + message.Length];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        message.CopyTo(span[offset..]);

        return buffer;
    }

    private static byte[] EncodeSubscribe(SubscribeCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[offset + queueName.Length];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        return buffer;
    }

    private static byte[] EncodeDelete(DeleteCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[offset + queueName.Length + CommandConstants.MessageIdSize];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        command.MessageId.TryWriteBytes(span[offset..]);

        return buffer;
    }

    private static void WriteQueueName(
        Span<byte> buffer,
        byte[] queueName)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer[..CommandConstants.QueueNameLengthSize], queueName.Length);

        queueName.CopyTo(buffer[CommandConstants.QueueNameLengthSize..]);
    }
}