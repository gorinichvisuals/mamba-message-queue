namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class QueueStorageService
{
    private const string QueuesDirectory = "queues";
    private const string QueueMetadataFile = "queue.dat";

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

        int size =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(long) +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(long) +
            sizeof(int) +
            name.Length;

        byte[] buffer = new byte[size];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = StorageVersion;

        queue.Id.TryWriteBytes(span[offset..]);
        offset += 16;

        span[offset++] = queue.IsDurable
            ? (byte)1
            : (byte)0;

        span[offset++] = queue.MessageRetention.RetentionEnabled
            ? (byte)1
            : (byte)0;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queue.MessageRetention.RetentionPeriod.Ticks);

        offset += sizeof(long);

        span[offset++] = queue.LogRetention.RetentionEnabled
            ? (byte)1
            : (byte)0;

        span[offset++] = (byte)queue.LogRetention.LogLevel;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queue.LogRetention.RetentionPeriod.Ticks);

        offset += sizeof(long);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], name.Length);

        offset += sizeof(int);

        name.CopyTo(span[offset..]);

        return buffer;
    }

    private static async Task<StoredMambaQueue> DeserializeQueue(Stream stream, CancellationToken cancellationToken)
    {
        const int headerSize =
            sizeof(byte) +
            16 +
            sizeof(byte) +
            sizeof(byte) +
            sizeof(long) +
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

        bool messageRetentionEnabled = span[offset++] is not 0;

        long messageRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(span[offset..]);

        offset += sizeof(long);

        TimeSpan messageRetentionPeriod = TimeSpan.FromTicks(messageRetentionPeriodTicks);

        bool logRetentionEnabled = span[offset++] is not 0;

        LogLevel logLevel = (LogLevel)span[offset++];

        long logRetentionPeriodTicks = BinaryPrimitives.ReadInt64BigEndian(span[offset..]);

        offset += sizeof(long);

        TimeSpan logRetentionPeriod = TimeSpan.FromTicks(logRetentionPeriodTicks);

        int nameLength = BinaryPrimitives.ReadInt32BigEndian(span[offset..]);

        if (nameLength < 0)
            throw new InvalidDataException("Queue name length cannot be negative.");

        byte[] nameBuffer = new byte[nameLength];

        await ReadExactly(stream, nameBuffer, cancellationToken);

        string name = Encoding.UTF8.GetString(nameBuffer);

        return new StoredMambaQueue(
            queueId,
            name,
            isDurable,
            new StoredMessageRetentionOptions(
                messageRetentionEnabled,
                messageRetentionPeriod),
            new StoredLogRetentionOptions(
                logRetentionEnabled,
                logLevel,
                logRetentionPeriod));
    }

    private static string GetQueuePath(Guid queueId)
        => $"{QueuesDirectory}/{queueId:N}";

    private static string GetQueueMetadataPath(Guid queueId)
        => $"{GetQueuePath(queueId)}/{QueueMetadataFile}";
}