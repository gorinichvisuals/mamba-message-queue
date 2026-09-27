namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
    public async Task WriteServerLog(string message, CancellationToken cancellationToken = default)
    {
        byte[] data = Encoding.UTF8.GetBytes($"{message}{Environment.NewLine}");

        List<(int Number, string Path)> segments = await GetServerLogSegments(cancellationToken);

        int currentSegment = 0;
        long currentSegmentSize = 0;

        if (segments.Count > 0)
        {
            (currentSegment, string path) = segments[^1];

            currentSegmentSize = await GetSegmentSize(path);
        }

        currentSegment = GetNextSegment(currentSegment, currentSegmentSize, data.Length, _serverLogSegmentSizeInBytes);

        await fileStorage.AppendToFile(GetServerLogSegmentPath(currentSegment), data, flushToDisk: true, cancellationToken);
    }
    
    public async Task CleanupServerLogs(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        List<(int Number, string Path)> segments = await GetServerLogSegments(cancellationToken);

        if (segments.Count <= 1)
            return;

        int activeSegment = segments[^1].Number;
        DateTime cutoff = DateTime.UtcNow - retentionPeriod;

        foreach ((int number, string path) in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (number == activeSegment)
                continue;

            DateTime lastWriteTimeUtc = fileStorage.GetLastWriteTimeUtc(path);

            if (lastWriteTimeUtc >= cutoff)
                continue;

            await fileStorage.DeleteFile(path);
        }
    }
    
    private async Task<List<(int Number, string Path)>> GetServerLogSegments(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> files = await fileStorage.GetFiles(GetServerLogsPath(), cancellationToken);

        return files
            .Select(ParseSegment)
            .Where(static segment => segment is not null)
            .Select(static segment => segment!.Value)
            .OrderBy(static segment => segment.Number)
            .ToList();
    }

    private async Task<long> GetSegmentSize(string path)
    {
        await using Stream stream = await fileStorage.OpenReadFile(path);

        return stream.Length;
    }

    private static int GetNextSegment(int currentSegment, long currentSegmentSize, int dataSize, int segmentSize)
    {
        if (currentSegmentSize > 0 && currentSegmentSize + dataSize > segmentSize)
            return currentSegment + 1;

        return currentSegment;
    }
    
    private static string GetServerLogsPath()
        => $"{ServerDirectory}/{LogsDirectory}";

    private static string GetServerLogSegmentPath(int segment)
        => $"{GetServerLogsPath()}/{SegmentPrefix}{segment:D6}{SegmentExtension}";
}