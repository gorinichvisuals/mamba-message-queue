namespace MambaMQ.Logging.Options;

public sealed class ServerLoggingOptions
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;    
    public bool ConsoleEnabled { get; init; } = true;
}