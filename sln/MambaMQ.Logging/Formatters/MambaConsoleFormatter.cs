namespace MambaMQ.Logging.Formatters;

internal sealed class MambaConsoleFormatter() : ConsoleFormatter("Mamba")
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string source = "Server";
        string? name = null;

        scopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is not MambaLogScope mambaScope) 
                return;
            
            source = mambaScope.Source;
            name = mambaScope.Name;
        },
        0);

        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception is not null)
            message += $"{Environment.NewLine}{logEntry.Exception}";

        string prefix = name is null
            ? $"[{source}]"
            : $"[{source}][{name}]";

        textWriter.WriteLine($"{prefix}[{DateTimeOffset.UtcNow:O}][{logEntry.LogLevel}][{message}]");
    }
}