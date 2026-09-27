namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
    private readonly int _serverLogSegmentSizeInBytes = serverLogSegmentSizeInBytes > 0
        ? serverLogSegmentSizeInBytes
        : throw new ArgumentOutOfRangeException(nameof(serverLogSegmentSizeInBytes));

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