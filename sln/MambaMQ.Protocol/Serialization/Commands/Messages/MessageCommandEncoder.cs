namespace MambaMQ.Protocol.Serialization.Commands.Messages;

internal static class MessageCommandEncoder
{
    internal static byte[] EncodePublishMessage(QueueMessagePublishCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] message = MessageEncoder.Encode(command.MambaMessage);

        int payloadSize =
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            message.Length;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.QueueName);

        message.CopyTo(span[offset..]);

        return buffer;
    }

    internal static byte[] EncodeDeleteMessage(QueueMessageDeleteCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        byte[] buffer = new byte[
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.MessageIdSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.QueueName);

        command.MessageId.TryWriteBytes(span[offset..]);

        return buffer;
    }

    internal static byte[] EncodeGetMessages(QueueMessagesGetCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        byte[] buffer = new byte[CommandConstants.QueueNameLengthSize + queueName.Length + sizeof(int) * 2];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.QueueName);

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, sizeof(int)), command.Skip);
        offset += sizeof(int);

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, sizeof(int)), command.Take);

        return buffer;
    }

    internal static byte[] EncodeDeleteMessages(QueueMessagesDeleteCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int messageCount = command.MessageIds.Count;

        byte[] buffer = new byte[
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            sizeof(int) +
            messageCount * CommandConstants.MessageIdSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.QueueName);

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset, sizeof(int)), messageCount);

        offset += sizeof(int);

        foreach (Guid messageId in command.MessageIds)
        {
            messageId.TryWriteBytes(span.Slice(offset, CommandConstants.MessageIdSize));
            offset += CommandConstants.MessageIdSize;
        }

        return buffer;
    }
}