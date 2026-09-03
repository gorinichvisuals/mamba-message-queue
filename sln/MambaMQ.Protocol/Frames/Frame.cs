namespace MambaMQ.Protocol.Frames;

public sealed class Frame(FrameType type, ReadOnlyMemory<byte> payload)
{
    public FrameType Type { get; }  = type;
    public ReadOnlyMemory<byte> Payload { get; }  = payload;
}

public enum FrameType : byte
{
    PublishMessage = 1,
    SubscribeQueue = 2,
    DeleteMessage = 3,
    GetMessage = 4,
}