namespace MambaMQ.Server.Options;

public sealed class ServerLoggingOptions
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;    
    public bool ConsoleEnabled { get; init; } = true;
    public bool CleanupLogEnabled { get; init; } = true;
    public bool FileEnabled { get; init; } = false;
    public int SegmentSizeInBytes { get; init; } = 32 * 1024 * 1024;
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(24);
}