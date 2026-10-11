namespace MambaMQ.Protocol.Serialization.Commands.Messages;

internal static class MessageCommandEncoder
{
    internal static byte[] EncodePublishMessage(PublishMessageCommand command)
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
    
    internal static byte[] EncodeDeleteMessage(DeleteMessageCommand command)
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
}