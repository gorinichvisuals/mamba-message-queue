namespace MambaMQ.Protocol.Enums;

public enum ErrorCode : byte
{
    None = 0,
    AuthenticationFailed = 1,
    InvalidArgument,
    PersistenceError,
    PermissionDenied,
    
    ExchangeNotFound,
    
    QueueNotFound,
}