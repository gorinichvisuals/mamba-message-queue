namespace MambaMQ.Protocol.Constants;

public static class CommandConstants
{
    public const int QueueIdSize = 16;
    public const int QueueNameLengthSize = 4;
    public const int MessageIdSize = 16;
    public const int IsDurableSize = sizeof(byte);
    public const int MessageRetentionEnabledSize = sizeof(byte);
    public const int MessageRetentionPeriodSize = sizeof(long);
    public const int UsernameLengthSize = sizeof(int);
    public const int PasswordLengthSize = sizeof(int);
    public const int LoadBalancingAlgorithmSize = sizeof(byte);
    public const int MaxMessagesSize = sizeof(int);
    public const int MaxBytesSize = sizeof(int);
    public const int MaxWaitTimeSize = sizeof(long);
    public const int WeightSize = sizeof(int);
    public const int ServiceNameLengthSize = sizeof(int);
    public const int PermissionsCountSize = sizeof(int);
    public const int PermissionSize = sizeof(byte);
    public const int ExchangeNameLengthSize = sizeof(int);
    public const int RoutingKeyLengthSize = sizeof(int);
    public const int ExchangeTypeSize = sizeof(byte);
}