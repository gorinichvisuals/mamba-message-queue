namespace MambaMQ.Protocol.Constants;

public static class CommandConstants
{
    public const int QueueNameLengthSize = 4;
    public const int MessageIdSize = 16;
    public const int IsDurableSize = sizeof(byte);
    public const int PersistMessagesSize = sizeof(byte);
}