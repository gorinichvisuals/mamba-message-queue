namespace MambaMQ.Protocol.Serialization.Messages;

public static class MessageEncoder
{
    public static byte[] Encode(MambaMessage message)
    {
        int bodyLength = message.Body.Length;

        byte[] buffer = new byte[MessageConstants.HeaderSize + bodyLength];
        Span<byte> span = buffer;

        message.MessageId.TryWriteBytes(span[..MessageConstants.MessageIdSize]);

        BinaryPrimitives.WriteInt64BigEndian(
            span.Slice(
                MessageConstants.MessageIdSize,
                MessageConstants.ReceivedAtSize),
            message.ReceivedAt.Ticks);

        BinaryPrimitives.WriteInt32BigEndian(
            span.Slice(
                MessageConstants.BodyLengthOffset,
                MessageConstants.BodyLengthSize),
            bodyLength);

        message.Body.Span.CopyTo(span[MessageConstants.HeaderSize..]);

        return buffer;
    }
}