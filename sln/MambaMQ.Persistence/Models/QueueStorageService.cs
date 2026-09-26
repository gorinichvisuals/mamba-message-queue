namespace MambaMQ.Persistence.Models;

public sealed class QueueStorageState(
    int currentSegment = 0, 
    long currentSegmentSize = 0,
    int currentLogSegment = 0,
    long currentLogSegmentSize = 0)
{
    public int CurrentSegment { get; set; } = currentSegment;
    public long CurrentSegmentSize { get; set; } = currentSegmentSize;
    public int CurrentLogSegment { get; set; } = currentLogSegment;
    public long CurrentLogSegmentSize { get; set; } = currentLogSegmentSize;
    public SemaphoreSlim Gate { get; } = new(1, 1);
}