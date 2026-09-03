namespace MambaMQ.Protocol.Serialization.Commands;

public static class CommandEncoder
{
    public static byte[] Encode(ICommand command)
    {
        return command.Type switch
        {
            FrameType.PublishMessage => EncodePublishMessage((PublishMessageCommand)command),
            FrameType.SubscribeQueue => EncodeSubscribeQueue((SubscribeQueueCommand)command),
            FrameType.DeleteMessage => EncodeDeleteMessage((DeleteMessageCommand)command),

            _ => throw new ArgumentException($"Unsupported command type: {command.Type}.", nameof(command))
        };
    }
    
    private static byte[] EncodePublishMessage(PublishMessageCommand messageCommand)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(messageCommand.QueueName);
        byte[] message = MessageEncoder.Encode(messageCommand.MambaMessage);

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[ offset + queueName.Length + message.Length];

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

        byte[] buffer = new byte[offset + queueName.Length + CommandConstants.AutoAcknowledgeSize];

        Span<byte> span = buffer;

        WriteQueueName(span, queueName);

        offset += queueName.Length;

        span[offset] = queueCommand.AutoAcknowledge 
            ? (byte)1 
            : (byte)0;

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

    private static void WriteQueueName(Span<byte> buffer, byte[] queueName)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer[..CommandConstants.QueueNameLengthSize], queueName.Length);

        queueName.CopyTo(buffer[CommandConstants.QueueNameLengthSize..]);
    }
}