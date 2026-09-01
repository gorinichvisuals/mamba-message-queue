namespace MambaMQ.Protocol.Serialization.Frames;

public static class FrameDecoder
{
    public static Frame Decode(ReadOnlySpan<byte> buffer)
    {
        ValidateHeaderSize(buffer);
        ValidateMagic(buffer);
        ValidateVersion(buffer);
        
        FrameType frameType = ReadFrameType(buffer);
        int payloadLength = ReadPayloadLength(buffer);
        
        ValidateFrameLength(buffer, payloadLength);
        
        byte[] payload = buffer
            .Slice(FrameConstants.HeaderSize, payloadLength)
            .ToArray();
        
        return new Frame(frameType, payload);
    }
    
    private static void ValidateHeaderSize(ReadOnlySpan<byte> buffer)
    {
        if(buffer.Length < FrameConstants.HeaderSize)
            throw new InvalidDataException("Frame is too short.");
    }

    private static void ValidateMagic(ReadOnlySpan<byte> buffer)
    {
        uint magic = BinaryPrimitives.ReadUInt32BigEndian(
            buffer.Slice(
                FrameConstants.MagicOffset, 
                FrameConstants.MagicSize));
        
        if (magic is not FrameConstants.Magic)
            throw new InvalidDataException("Invalid frame magic.");
    }

    private static void ValidateVersion(ReadOnlySpan<byte> buffer)
    {
        byte version = buffer[FrameConstants.VersionOffset];
        
        if (version is not FrameConstants.Version)
            throw new InvalidDataException("Unsupported protocol version.");
    }

    private static FrameType ReadFrameType(ReadOnlySpan<byte> buffer)
    {
        byte value = buffer[FrameConstants.FrameTypeOffset];
        
        if (!Enum.IsDefined((FrameType)value))
            throw new InvalidDataException("Unknown frame type.");
        
        return (FrameType)value;
    }

    private static int ReadPayloadLength(ReadOnlySpan<byte> buffer)
    {
        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(
            buffer.Slice(
                FrameConstants.PayloadLengthOffset,
                FrameConstants.PayloadLengthSize));

        return payloadLength switch
        {
            < 0 => throw new InvalidDataException("Invalid payload length."),
            > FrameConstants.MaxPayloadSize => throw new InvalidDataException("Payload is too large."),
            _ => payloadLength
        };
    }

    private static void ValidateFrameLength(ReadOnlySpan<byte> buffer, int payloadLength)
    {
        int expectedLength = FrameConstants.HeaderSize + payloadLength;

        if (buffer.Length != expectedLength)
            throw new InvalidDataException("Invalid frame length.");
    }
}