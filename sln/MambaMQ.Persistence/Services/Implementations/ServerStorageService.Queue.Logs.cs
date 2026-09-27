namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
   public async Task SaveQueueLog(Guid queueId, StoredQueueLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        QueueStorageState state = GetOrCreateState(queueId);

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            byte[] record = SerializeLog(log);

            await AppendLogRecord(queueId, state, record, cancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }
   
    public async Task CleanupQueueLogs(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> directories = await fileStorage.GetDirectories(QueuesDirectory, cancellationToken);

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParseExact(directory, "N", out Guid queueId))
                continue;

            QueueStorageState state = GetOrCreateState(queueId);

            await state.Gate.WaitAsync(cancellationToken);

            try
            {
                StoredMambaQueue queue = await RestoreQueueMetadata(queueId, cancellationToken);

                if (!queue.LogRetention.RetentionEnabled)
                    continue;

                await CleanupLogsForQueue(queueId, queue.LogRetention.RetentionPeriod, cancellationToken);
            }
            finally
            {
                state.Gate.Release();
            }
        }
    }
    
    private async Task AppendLogRecord(
        Guid queueId,
        QueueStorageState state,
        byte[] record,
        CancellationToken cancellationToken)
    {
        if (state.CurrentLogSegmentSize > 0 && state.CurrentLogSegmentSize + record.Length > _queueLogSegmentSizeInBytes)
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

        int recordLength = QueueLogTimestampSize + QueueLogLevelSize + QueueLogEventTypeSize + QueueLogMessageLengthSize + message.Length;

        byte[] buffer = new byte[MessageRecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += MessageRecordLengthSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], log.Timestamp.UtcTicks);

        offset += QueueLogTimestampSize;

        span[offset++] = log.Level;

        span[offset++] = log.EventType;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], message.Length);

        offset += QueueLogMessageLengthSize;

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