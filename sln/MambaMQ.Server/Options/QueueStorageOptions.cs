namespace MambaMQ.Server.Options;

public sealed class QueueStorageOptions
{
    public string Path { get; init; } = "data";
    public bool MessageCleanupEnabled { get; init; } = false;
    public int MessageSegmentSizeInBytes { get; init; } = 16777216;
    public int MaxMessageSegments { get; init; } = 2;
    public TimeSpan CleanupMessageInterval { get; init; } = TimeSpan.FromMinutes(10);
}