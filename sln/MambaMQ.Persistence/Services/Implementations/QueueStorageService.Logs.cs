namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class QueueStorageService
{
    private const string LogsDirectory = "logs";

    private const int LogTimestampSize = sizeof(long);
    private const int LogLevelSize = sizeof(byte);
    private const int LogEventTypeSize = sizeof(byte);
    private const int LogMessageLengthSize = sizeof(int);
    
    private readonly int _logSegmentSizeInBytes = logSegmentSizeInBytes > 0
            ? logSegmentSizeInBytes
            : throw new ArgumentOutOfRangeException(nameof(logSegmentSizeInBytes));
    
    private async Task AppendLogRecord(
        Guid queueId,
        QueueStorageState state,
        byte[] record,
        CancellationToken cancellationToken)
    {
        if (state.CurrentLogSegmentSize > 0 && state.CurrentLogSegmentSize + record.Length > _logSegmentSizeInBytes)
        {
            state.CurrentLogSegment++;
            state.CurrentLogSegmentSize = 0;
        }

        string path = GetLogSegmentPath(queueId, state.CurrentLogSegment);

        await fileStorage.AppendToFile(path, record, flushToDisk: true, cancellationToken);

        state.CurrentLogSegmentSize += record.Length;
    }
    
    private static byte[] SerializeLog(StoredQueueLog log)
    {
        byte[] message = Encoding.UTF8.GetBytes(log.Message);

        int recordLength = LogTimestampSize + LogLevelSize + LogEventTypeSize + LogMessageLengthSize + message.Length;

        byte[] buffer = new byte[RecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += RecordLengthSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], log.Timestamp.UtcTicks);

        offset += LogTimestampSize;

        span[offset++] = log.Level;

        span[offset++] = log.EventType;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], message.Length);

        offset += LogMessageLengthSize;

        message.CopyTo(span[offset..]);

        return buffer;
    }
    
    private async Task CleanupLogsForQueue(
        Guid queueId,
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> files = await fileStorage.GetFiles(GetLogsPath(queueId), cancellationToken);

        List<(int Number, string Path)> segments = files
            .Select(ParseSegment)
            .Where(static segment => segment is not null)
            .Select(static segment => segment!.Value)
            .OrderBy(static segment => segment.Number)
            .ToList();

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
    
    private static string GetLogsPath(Guid queueId)
        => $"{GetQueuePath(queueId)}/{LogsDirectory}";

    private static string GetLogSegmentPath(Guid queueId, int segment)
        => $"{GetLogsPath(queueId)}/{SegmentPrefix}{segment:D6}{SegmentExtension}";
}