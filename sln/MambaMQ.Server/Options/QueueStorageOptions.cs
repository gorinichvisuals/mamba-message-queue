namespace MambaMQ.Server.Options;

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