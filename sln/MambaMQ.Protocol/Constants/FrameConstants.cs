namespace MambaMQ.Protocol.Constants;

public static class FrameConstants
{
    public const ushort Magic = 0x4D4D;
    public const int MagicSize = 2;
    public const int MagicOffset = 0;
    
    public const byte Version = 1;
    private const int VersionSize = 1;
    public const int VersionOffset = MagicOffset + MagicSize;
    
    private const int FrameTypeSize = 1;
    public const int FrameTypeOffset = VersionOffset + VersionSize;
    
    public const int PayloadLengthSize = 4;
    public const int PayloadLengthOffset = FrameTypeOffset + FrameTypeSize;
    
    public const int HeaderSize = MagicSize + VersionSize + FrameTypeSize + PayloadLengthSize;
    public const int MaxPayloadSize = 16 * 1024 * 1024;
}