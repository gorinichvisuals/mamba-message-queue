namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class ServerStorageService
{
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

            SetState(queueId, new QueueStorageState(currentSegment, currentSegmentSize));

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
    
    private async Task<StoredMambaQueue> RestoreQueueMetadata(Guid queueId, CancellationToken cancellationToken)
    {
        string path = GetQueueMetadataPath(queueId);

        await using Stream stream = await fileStorage.OpenReadFile(path);

        StoredMambaQueue storedQueue = await DeserializeQueue(stream, cancellationToken);

        return storedQueue.Id != queueId
            ? throw new InvalidDataException($"Queue metadata ID '{storedQueue.Id}' does not match directory ID '{queueId}'.")
            : storedQueue;
    }

    private static byte[] SerializeQueue(StoredMambaQueue queue)
    {
        byte[] name = Encoding.UTF8.GetBytes(queue.Name);

        int permissionsSize = sizeof(int);

        foreach ((string serviceName, byte permission) in queue.Permissions)
        {
            byte[] serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);

            permissionsSize += sizeof(int) + serviceNameBytes.Length + sizeof(byte);
        }

        int size =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(long) +
            sizeof(int) +
            name.Length +
            permissionsSize;

        byte[] buffer = new byte[size];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = StorageVersion;

        queue.Id.TryWriteBytes(span[offset..]);
        offset += 16;

        span[offset++] = queue.IsDurable
            ? (byte)1
            : (byte)0;

        span[offset++] = queue.LoadBalancingAlgorithm;

        span[offset++] = queue.MessageRetention.RetentionEnabled
            ? (byte)1
            : (byte)0;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queue.MessageRetention.RetentionPeriod.Ticks);

        offset += sizeof(long);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], name.Length);

        offset += sizeof(int);

        name.CopyTo(span[offset..]);
        offset += name.Length;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], queue.Permissions.Count);

        offset += sizeof(int);

        foreach ((string serviceName, byte permission) in queue.Permissions)
        {
            byte[] serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], serviceNameBytes.Length);

            offset += sizeof(int);

            serviceNameBytes.CopyTo(span[offset..]);
            offset += serviceNameBytes.Length;

            span[offset++] = permission;
        }

        return buffer;
    }

    private static async Task<StoredMambaQueue> DeserializeQueue(
        Stream stream,
        CancellationToken cancellationToken)
    {
        const int headerSize =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(long) +
            sizeof(int);

        byte[] header = new byte[headerSize];

        await ReadExactly(stream, header, cancellationToken);

        ReadOnlySpan<byte> span = header;

        int offset = 0;

        byte version = span[offset++];

        if (version is not StorageVersion)
            throw new InvalidDataException($"Unsupported storage version '{version}'.");

        Guid queueId = new(span.Slice(offset, 16));

        offset += 16;

        bool isDurable = span[offset++] is not 0;

        byte loadBalancingAlgorithm = span[offset++];

        bool messageRetentionEnabled = span[offset++] is not 0;

        long messageRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(span[offset..]);

        offset += sizeof(long);

        TimeSpan messageRetentionPeriod = TimeSpan.FromTicks(messageRetentionPeriodTicks);

        int nameLength = BinaryPrimitives.ReadInt32BigEndian(span[offset..]);

        if (nameLength < 0)
            throw new InvalidDataException("Queue name length cannot be negative.");

        byte[] nameBuffer = new byte[nameLength];

        await ReadExactly(stream, nameBuffer, cancellationToken);

        string name = Encoding.UTF8.GetString(nameBuffer);

        byte[] permissionsCountBuffer = new byte[sizeof(int)];

        await ReadExactly(stream, permissionsCountBuffer, cancellationToken);

        int permissionsCount = BinaryPrimitives.ReadInt32BigEndian(permissionsCountBuffer);

        if (permissionsCount < 0)
            throw new InvalidDataException("Queue permissions count cannot be negative.");

        Dictionary<string, byte> permissions = new(permissionsCount);

        for (int i = 0; i < permissionsCount; i++)
        {
            await ReadExactly(stream, permissionsCountBuffer, cancellationToken);

            int serviceNameLength = BinaryPrimitives.ReadInt32BigEndian(permissionsCountBuffer);

            if (serviceNameLength <= 0)
                throw new InvalidDataException("Service name length must be greater than zero.");

            byte[] serviceNameBuffer = new byte[serviceNameLength];

            await ReadExactly(stream, serviceNameBuffer, cancellationToken);

            string serviceName = Encoding.UTF8.GetString(serviceNameBuffer);

            int permission = stream.ReadByte();

            if (permission < 0)
                throw new EndOfStreamException();

            permissions.Add(serviceName, (byte)permission);
        }

        return new StoredMambaQueue(
            queueId,
            name,
            isDurable,
            loadBalancingAlgorithm,
            permissions,
            new StoredMessageRetentionOptions(
                messageRetentionEnabled,
                messageRetentionPeriod));
    }

    private static string GetQueuePath(Guid queueId)
        => $"{QueuesDirectory}/{queueId:N}";

    private static string GetQueueMetadataPath(Guid queueId)
        => $"{GetQueuePath(queueId)}/{QueueMetadataFile}";
}