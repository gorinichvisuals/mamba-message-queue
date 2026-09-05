namespace MambaMQ.Server.Readers;

public static class FrameReader
{
    public static async Task<Frame> ReadFrameAsync(NetworkStream stream, int maxMessageSizeInKilobytes, CancellationToken cancellationToken)
    {
        byte[] header = new byte[FrameConstants.HeaderSize];

        await ReadExactlyAsync(stream, header, cancellationToken);

        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(FrameConstants.PayloadLengthOffset, FrameConstants.PayloadLengthSize));

        if (payloadLength < 0)
            throw new InvalidDataException("Invalid payload length.");

        if (payloadLength > maxMessageSizeInKilobytes)
            throw new InvalidDataException(
                $"Payload is too large. Maximum size is {maxMessageSizeInKilobytes} bytes.");

        byte[] buffer = new byte[FrameConstants.HeaderSize + payloadLength];

        header.CopyTo(buffer, 0);

        if (payloadLength > 0)
            await ReadExactlyAsync(stream, buffer.AsMemory(FrameConstants.HeaderSize, payloadLength), cancellationToken);

        return FrameDecoder.Decode(buffer);
    }
    
    private static async Task ReadExactlyAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

            if (bytesRead is 0)
                throw new IOException("Client disconnected.");

            buffer = buffer[bytesRead..];
        }
    }
}