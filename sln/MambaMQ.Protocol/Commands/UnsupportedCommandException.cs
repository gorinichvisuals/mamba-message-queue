namespace MambaMQ.Protocol.Commands;

public sealed class UnsupportedCommandException(FrameType frameType)
    : Exception($"Unsupported command frame type: {(byte)frameType} ({frameType}).")
{
    public FrameType FrameType { get; } = frameType;
}