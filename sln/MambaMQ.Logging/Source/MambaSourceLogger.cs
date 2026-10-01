namespace MambaMQ.Logging.Source;

internal sealed class MambaSourceLogger(ILogger logger, string source) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        using IDisposable? scope = logger.BeginScope(new MambaLogScope(source));

        logger.Log(logLevel, eventId, state, exception, formatter);
    }
}