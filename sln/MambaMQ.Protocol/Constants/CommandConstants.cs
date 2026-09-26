namespace MambaMQ.Protocol.Constants;

public static class CommandConstants
{
    public const int QueueNameLengthSize = 4;
    public const int MessageIdSize = 16;
    public const int IsDurableSize = sizeof(byte);
    public const int MessageRetentionEnabledSize = sizeof(byte);
    public const int MessageRetentionPeriodSize = sizeof(long);
    public const int LogsRetentionEnabledSize = 1;
    public const int LogsRetentionPeriodSize = sizeof(long);
    public const int LogLevelSize = sizeof(byte);
}