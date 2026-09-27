namespace MambaMQ.Server.Logging.Implementations;

internal sealed class ServerFileLogger(IServerStorageService storage) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);

        if (exception is not null)
            message += $"{Environment.NewLine}{exception}";

        storage.WriteServerLog($"[{logLevel}] {message}");
    }
}