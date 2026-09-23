namespace MambaMQ.Persistence.Models;

public sealed class QueueStorageState(int currentSegment = 0, long currentSegmentSize = 0)
{
    public int CurrentSegment { get; set; } = currentSegment;
    public long CurrentSegmentSize { get; set; } = currentSegmentSize;
    public SemaphoreSlim Gate { get; } = new(1, 1);
}