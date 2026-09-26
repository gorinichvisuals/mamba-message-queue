namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class QueueStorageService(
    IFileStorageService fileStorage, 
    int messageSegmentSizeInBytes,
    int maxMessageSegments,
    int logSegmentSizeInBytes) : IQueueStorageService
{
    private const byte StorageVersion = 1;

    public async Task SaveQueue(StoredMambaQueue storedQueue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storedQueue);

        byte[] data = SerializeQueue(storedQueue);
        await fileStorage.ReplaceFile(GetQueueMetadataPath(storedQueue.Id), data, flushToDisk: true, cancellationToken);
        GetOrCreateState(storedQueue.Id);
    }

    public async Task<IReadOnlyCollection<StoredMambaQueueState>> RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> directories = await fileStorage.GetDirectories(QueuesDirectory, cancellationToken);

        List<StoredMambaQueueState> queues = [];

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParseExact(directory, "N", out Guid queueId))
                continue;

            StoredMambaQueue storedQueue = await RestoreQueueMetadata(queueId, cancellationToken);

            Dictionary<Guid, StoredMambaMessage> messages = [];
            List<Guid> messageOrder = [];

            IReadOnlyCollection<string> files = await fileStorage.GetFiles(GetMessagesPath(queueId), cancellationToken);

            List<(int Number, string Path)> segments = files
                .Select(ParseSegment)
                .Where(static segment => segment is not null)
                .Select(static segment => segment!.Value)
                .OrderBy(static segment => segment.Number)
                .ToList();

            int currentSegment = 0;
            long currentSegmentSize = 0;

            foreach ((int number, string path) in segments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using Stream stream = await fileStorage.OpenReadFile(path);

                await ReplaySegment(stream, messages, messageOrder, cancellationToken);

                currentSegment = number;
                currentSegmentSize = stream.Length;
            }

            int currentLogSegment = 0;
            long currentLogSegmentSize = 0;

            IReadOnlyCollection<string> logFiles = await fileStorage.GetFiles(GetLogsPath(queueId), cancellationToken);

            List<(int Number, string Path)> logSegments = logFiles
                .Select(ParseSegment)
                .Where(static segment => segment is not null)
                .Select(static segment => segment!.Value)
                .OrderBy(static segment => segment.Number)
                .ToList();

            if (logSegments.Count > 0)
            {
                (currentLogSegment, string path) = logSegments[^1];

                await using Stream stream = await fileStorage.OpenReadFile(path);

                currentLogSegmentSize = stream.Length;
            }

            SetState(queueId, new QueueStorageState(
                currentSegment,
                currentSegmentSize,
                currentLogSegment,
                currentLogSegmentSize));

            List<StoredMambaMessage> restoredMessages = [];

            foreach (Guid messageId in messageOrder)
                if (messages.TryGetValue(messageId, out StoredMambaMessage? message))
                    restoredMessages.Add(message);

            queues.Add(new StoredMambaQueueState(storedQueue, restoredMessages));
        }

        return queues;
    }

    public async Task DeleteQueue(Guid queueId, CancellationToken cancellationToken = default)
    {
        QueueStorageState? state = GetState(queueId);

        if (state is not null) 
            await state.Gate.WaitAsync(cancellationToken);

        try
        {
            await fileStorage.DeleteDirectory(GetQueuePath(queueId), cancellationToken);

            RemoveState(queueId);
        }
        finally
        {
            state?.Gate.Release();
        }
    }

    public async Task SaveMessage(Guid queueId, StoredMambaMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        QueueStorageState state = GetOrCreateState(queueId);

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            byte[] record = SerializeMessage(message);

            await AppendRecord(queueId, state, record, cancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public async Task MarkAsDeleteMessage(Guid queueId, Guid messageId, CancellationToken cancellationToken = default)
    {
        QueueStorageState state = GetOrCreateState(queueId);

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            byte[] record = SerializeDelete(messageId);

            await AppendRecord(queueId, state, record, cancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }
    
    public async Task SaveLog(Guid queueId, StoredQueueLog log, CancellationToken cancellationToken = default)
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

    public async Task CleanupMessages(CancellationToken cancellationToken = default)
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

                if (!queue.MessageRetention.RetentionEnabled)
                    continue;

                await CleanupMessagesForQueue(queueId, queue.MessageRetention.RetentionPeriod, cancellationToken);
            }
            finally
            {
                state.Gate.Release();
            }
        }
    }
    
    public async Task CleanupLogs(CancellationToken cancellationToken = default)
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
}