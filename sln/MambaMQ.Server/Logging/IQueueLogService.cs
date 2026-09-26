namespace MambaMQ.Server.Logging;

public interface IQueueLogService
{
    Task Log(
        MambaQueue queue, 
        LogLevel level, 
        LogEventType eventType, 
        string message, 
        CancellationToken cancellationToken = default);
}