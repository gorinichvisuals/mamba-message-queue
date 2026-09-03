namespace MambaMQ.Protocol.Constants;

public static class MessageConstants
{
    public const int MessageIdSize = 16;
    public const int ReceivedAtSize = 8;
    public const int BodyLengthSize = 4;

    public const int BodyLengthOffset = MessageIdSize + ReceivedAtSize;
    public const int HeaderSize = MessageIdSize + ReceivedAtSize + BodyLengthSize;
}