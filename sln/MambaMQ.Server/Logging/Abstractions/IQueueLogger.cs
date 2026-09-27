namespace MambaMQ.Server.Logging.Abstractions;

public interface IQueueLogger
{
    Task Log(
        MambaQueue queue, 
        MambaServerLogLevel level, 
        LogEventType eventType, 
        string message, 
        CancellationToken cancellationToken = default);
}