namespace MambaMQ.Protocol.Serialization.Commands.Messages;

internal static class MessageCommandDecoder
{
    internal static PublishMessageCommand DecodePublish(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Publish message command");

        MambaMessage message = MessageDecoder.Decode(buffer[offset..]);

        return new PublishMessageCommand(queueName, message);
    }

    internal static DeleteMessageCommand DecodeDelete(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "queue name",
            "Delete message command");

        if (buffer.Length - offset < CommandConstants.MessageIdSize)
            throw new InvalidDataException("Command does not contain MessageId.");

        Guid messageId = new(buffer.Slice(offset, CommandConstants.MessageIdSize));

        return new DeleteMessageCommand(queueName, messageId);
    }
}