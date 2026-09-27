namespace MambaMQ.Server.Options;

public sealed class MambaServerOptions
{
    public int Port { get; init; } = 24;
    public int MaxMessageSizeInBytes { get; init; } = 1 * 1024 * 1024;
    public QueueStorageOptions QueueStorage { get; init; } = new();
    public ServerLoggingOptions ServerLogging { get; init; } = new();
}

public sealed class QueueStorageOptions
{
    public string Path { get; init; } = "data";
    public bool MessageCleanupEnabled { get; init; } = true;
    public bool LogCleanupEnabled { get; init; } = true;
    public int MessageSegmentSizeInBytes { get; init; } = 16 * 1024 * 1024;
    public int LogSegmentSizeInBytes { get; init; } = 32 * 1024 * 1024;
    public int MaxMessageSegments { get; init; } = 2;
    public TimeSpan CleanupMessageInterval { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan CleanupLogInterval { get; init; } = TimeSpan.FromHours(24);
}

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