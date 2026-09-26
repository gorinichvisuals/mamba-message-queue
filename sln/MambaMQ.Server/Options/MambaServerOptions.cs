namespace MambaMQ.Server.Options;

public sealed class MambaServerOptions
{
    public int Port { get; init; } = 24;
    public int MaxMessageSizeInBytes { get; init; } = 1 * 1024 * 1024;
    public required StorageOptions Storage { get; init; }
}

public sealed class StorageOptions
{
    public required string Path { get; init; } = "data";
    public int MessageSegmentSizeInBytes { get; init; } = 32 * 1024 * 1024;
    public int LogSegmentSizeInBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxMessageSegments { get; init; } = 2;
    public TimeSpan CleanupMessagesInterval { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan CleanupLogsInterval { get; init; } = TimeSpan.FromHours(24);
}