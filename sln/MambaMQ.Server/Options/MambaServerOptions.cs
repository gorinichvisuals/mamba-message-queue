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
    public int SegmentSizeInBytes { get; init; } = 32 * 1024 * 1024;
    public int MaxSegments { get; init; } = 2;
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(5);
}