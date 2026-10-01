namespace MambaMQ.Logging.Formatters;

internal sealed class MambaConsoleFormatter() : ConsoleFormatter("Mamba")
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string source = "Server";

        scopeProvider?.ForEachScope(
            (scope, _) =>
            {
                if (scope is MambaLogScope mambaScope)
                    source = mambaScope.Source;
            },
            0);

        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception is not null)
            message += $"{Environment.NewLine}{logEntry.Exception}";

        textWriter.WriteLine($"[{source}][{DateTimeOffset.UtcNow:O}][{logEntry.LogLevel}][{message}]");
    }
}