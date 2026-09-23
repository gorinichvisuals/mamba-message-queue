namespace MambaMQ.Persistence.Services.Implementations;

internal sealed partial class QueueStorageService
{
    private const string QueuesDirectory = "queues";
    private const string MessagesDirectory = "messages";
    private const string QueueMetadataFile = "queue.dat";

    private const string SegmentPrefix = "segment-";
    private const string SegmentExtension = ".dat";

    private const byte StorageVersion = 1;

    private const int RecordLengthSize = sizeof(int);
    private const int RecordTypeSize = sizeof(byte);
    private const int MessageIdSize = 16;
    private const int ReceivedAtSize = sizeof(long);
    private const int BodyLengthSize = sizeof(int);
    
    private readonly int _segmentSizeInBytes = segmentSizeInBytes > 0
        ? segmentSizeInBytes
        : throw new ArgumentOutOfRangeException(nameof(segmentSizeInBytes));

    private readonly int _maxSegments = maxSegments > 0
        ? maxSegments
        : throw new ArgumentOutOfRangeException(nameof(maxSegments));
    
    private readonly ConcurrentDictionary<Guid, QueueStorageState> _states = [];
    
    private async Task CleanupQueue(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> files = await fileStorage.GetFiles(GetMessagesPath(queueId), cancellationToken);

        List<(int Number, string Path)> segments = files
            .Select(ParseSegment)
            .Where(static segment => segment is not null)
            .Select(static segment => segment!.Value)
            .OrderBy(static segment => segment.Number)
            .ToList();

        if (segments.Count <= _maxSegments)
            return;

        int activeSegment = segments[^1].Number;

        Dictionary<int, HashSet<Guid>> messagesBySegment = [];
        Dictionary<Guid, int> deleteSegments = [];

        foreach ((int number, string path) in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using Stream stream = await fileStorage.OpenReadFile(path);

            await AnalyzeSegment(stream, number, messagesBySegment, deleteSegments, cancellationToken);
        }

        foreach ((int number, string path) in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (number == activeSegment)
                continue;

            if (!messagesBySegment.TryGetValue(number, out HashSet<Guid>? messageIds))
                continue;

            bool allMessagesDeleted = messageIds.All(
                messageId => deleteSegments.TryGetValue(messageId, out int deleteSegment) && deleteSegment >= number);

            if (!allMessagesDeleted)
                continue;
            
            await fileStorage.DeleteFile(path);
        }
    }
    
    private static async Task AnalyzeSegment(
        Stream stream,
        int segmentNumber,
        Dictionary<int, HashSet<Guid>> messagesBySegment,
        Dictionary<Guid, int> deleteSegments,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> messageIds = [];

        messagesBySegment[segmentNumber] = messageIds;

        byte[] lengthBuffer = new byte[RecordLengthSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead = await ReadAtMost(stream, lengthBuffer, cancellationToken);

            switch (bytesRead)
            {
                case 0:
                case < RecordLengthSize:
                    return;
            }

            int recordLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

            if (recordLength <= 0)
                throw new InvalidDataException($"Invalid record length '{recordLength}'.");

            byte[] record = new byte[recordLength];

            bytesRead = await ReadAtMost(stream, record, cancellationToken);

            if (bytesRead < recordLength)
                return;

            AnalyzeRecord(record, messageIds, segmentNumber, deleteSegments);
        }
    }
    
    private static void AnalyzeRecord(
        ReadOnlySpan<byte> record,
        HashSet<Guid> messageIds,
        int segmentNumber,
        Dictionary<Guid, int> deleteSegments)
    {
        if (record.Length < RecordTypeSize)
            throw new InvalidDataException("Storage record is too small.");

        int offset = 0;

        StorageRecordType recordType = (StorageRecordType)record[offset++];

        switch (recordType)
        {
            case StorageRecordType.Message:
            {
                const int minimumLength = RecordTypeSize + MessageIdSize + ReceivedAtSize + BodyLengthSize;

                if (record.Length < minimumLength)
                    throw new InvalidDataException("Message record is too small.");

                Guid messageId = new(record.Slice(offset, MessageIdSize));

                messageIds.Add(messageId);

                break;
            }

            case StorageRecordType.DeleteMessage:
            {
                const int expectedLength = RecordTypeSize + MessageIdSize;

                if (record.Length != expectedLength)
                    throw new InvalidDataException("Invalid delete message record length.");

                Guid messageId = new(record.Slice(offset, MessageIdSize));

                deleteSegments[messageId] = segmentNumber;

                break;
            }

            default:
                throw new InvalidDataException($"Unknown storage record type '{recordType}'.");
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
    
    private async Task AppendRecord(Guid queueId, QueueStorageState state, byte[] record, CancellationToken cancellationToken)
    {
        if (state.CurrentSegmentSize > 0 && state.CurrentSegmentSize + record.Length > _segmentSizeInBytes)
        {
            state.CurrentSegment++;
            state.CurrentSegmentSize = 0;
        }

        string path = GetSegmentPath(queueId, state.CurrentSegment);

        await fileStorage.AppendToFile(path, record, flushToDisk: true, cancellationToken);

        state.CurrentSegmentSize += record.Length;
    }
    
    private static byte[] SerializeQueue( StoredMambaQueue queue)
    {
        byte[] name = Encoding.UTF8.GetBytes(queue.Name);

        int size = sizeof(byte) + 16 + sizeof(byte) + sizeof(byte) + sizeof(int) + name.Length;

        byte[] buffer = new byte[size];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = StorageVersion;

        queue.Id.TryWriteBytes(span[offset..]);
        offset += 16;

        span[offset++] = queue.IsDurable 
            ? (byte)1 
            : (byte)0;

        span[offset++] = queue.PersistMessages 
            ? (byte)1 
            : (byte)0;

        BinaryPrimitives.WriteInt32BigEndian( span[offset..], name.Length);

        offset += sizeof(int);

        name.CopyTo(span[offset..]);

        return buffer;
    }
    
    private static async Task<StoredMambaQueue> DeserializeQueue(Stream stream, CancellationToken cancellationToken)
    {
        const int headerSize = sizeof(byte) + 16 + sizeof(byte) + sizeof(byte) + sizeof(int);

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

        bool persistMessages = span[offset++] is not 0;

        int nameLength = BinaryPrimitives.ReadInt32BigEndian(span[offset..]);

        if (nameLength < 0)
            throw new InvalidDataException("Queue name length cannot be negative.");

        byte[] nameBuffer = new byte[nameLength];

        await ReadExactly(stream, nameBuffer, cancellationToken);

        string name = Encoding.UTF8.GetString(nameBuffer);

        return new StoredMambaQueue(queueId, name, isDurable, persistMessages);
    }
    
    private static byte[] SerializeMessage( StoredMambaMessage message)
    {
        int recordLength = RecordTypeSize + MessageIdSize + ReceivedAtSize + BodyLengthSize + message.Body.Length;

        byte[] buffer = new byte[RecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += RecordLengthSize;

        span[offset++] = (byte)StorageRecordType.Message;

        message.Id.TryWriteBytes(span[offset..]);

        offset += MessageIdSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], message.ReceivedAt.UtcTicks);

        offset += ReceivedAtSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], message.Body.Length);

        offset += BodyLengthSize;

        message.Body.Span.CopyTo(span[offset..]);

        return buffer;
    }
    
    private static byte[] SerializeDelete(Guid messageId)
    {
        const int recordLength = RecordTypeSize + MessageIdSize;

        byte[] buffer = new byte[RecordLengthSize + recordLength];

        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], recordLength);

        offset += RecordLengthSize;

        span[offset++] = (byte)StorageRecordType.DeleteMessage;

        messageId.TryWriteBytes(span[offset..]);

        return buffer;
    }
    
    private static async Task ReplaySegment(
        Stream stream,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder,
        CancellationToken cancellationToken)
    {
        byte[] lengthBuffer = new byte[RecordLengthSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead = await ReadAtMost(stream, lengthBuffer, cancellationToken);

            switch (bytesRead)
            {
                case 0:
                case < RecordLengthSize:
                    return;
            }

            int recordLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);

            if (recordLength <= 0)
                throw new InvalidDataException($"Invalid record length '{recordLength}'.");

            byte[] record = new byte[recordLength];

            bytesRead = await ReadAtMost(stream, record, cancellationToken);

            if (bytesRead < recordLength)
                return;

            ParseRecord(record, messages, messageOrder);
        }
    }
    
    private static void ParseRecord(
        ReadOnlySpan<byte> record,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder)
    {
        if (record.Length < RecordTypeSize)
            throw new InvalidDataException("Storage record is too small.");

        int offset = 0;

        StorageRecordType recordType = (StorageRecordType)record[offset++];

        switch (recordType)
        {
            case StorageRecordType.Message:
                ParseMessageRecord(record, ref offset, messages, messageOrder);
                break;

            case StorageRecordType.DeleteMessage:
                ParseDeleteRecord(record, ref offset, messages);
                break;

            default:
                throw new InvalidDataException(
                    $"Unknown storage record type '{recordType}'.");
        }
    }
    
    private static void ParseMessageRecord(
        ReadOnlySpan<byte> record,
        ref int offset,
        Dictionary<Guid, StoredMambaMessage> messages,
        List<Guid> messageOrder)
    {
        const int minimumLength = RecordTypeSize + MessageIdSize + ReceivedAtSize + BodyLengthSize;

        if (record.Length < minimumLength)
            throw new InvalidDataException("Message record is too small.");

        Guid messageId = new(record.Slice(offset, MessageIdSize));

        offset += MessageIdSize;

        long receivedAtTicks = BinaryPrimitives.ReadInt64BigEndian(record[offset..]);

        offset += ReceivedAtSize;

        int bodyLength = BinaryPrimitives.ReadInt32BigEndian(record[offset..]);

        offset += BodyLengthSize;

        if (bodyLength < 0 || bodyLength > record.Length - offset)
            throw new InvalidDataException("Invalid message body length.");

        byte[] body = record
            .Slice(offset, bodyLength)
            .ToArray();

        StoredMambaMessage message = new(messageId, new DateTimeOffset(receivedAtTicks, TimeSpan.Zero), body);

        if (!messages.ContainsKey(messageId))
            messageOrder.Add(messageId);

        messages[messageId] = message;
    }
    
    private static void ParseDeleteRecord(ReadOnlySpan<byte> record, ref int offset, Dictionary<Guid, StoredMambaMessage> messages)
    {
        const int expectedLength = RecordTypeSize + MessageIdSize;

        if (record.Length != expectedLength)
            throw new InvalidDataException("Invalid delete message record length.");

        Guid messageId = new(record.Slice(offset, MessageIdSize));

        messages.Remove(messageId);
    }
    
    private static (int Number, string Path)? ParseSegment(string path)
    {
        string fileName = Path.GetFileName(path);

        if (!fileName.StartsWith(SegmentPrefix, StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(SegmentExtension, StringComparison.OrdinalIgnoreCase))
            return null;

        string numberText = fileName[SegmentPrefix.Length.. ^SegmentExtension.Length];

        if (!int.TryParse(numberText, out int number))
            return null;

        return (number, path);
    }
    
    private static async Task<int> ReadAtMost(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);

            if (read is 0)
                break;

            totalRead += read;
        }

        return totalRead;
    }
    
    private static async Task ReadExactly(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);

            if (read is 0)
                throw new EndOfStreamException("Unexpected end of storage file.");

            totalRead += read;
        }
    }
    
    private QueueStorageState GetOrCreateState(Guid queueId)
        => _states.GetOrAdd(queueId, static _ => new QueueStorageState());

    private QueueStorageState? GetState(Guid queueId)
        => _states.GetValueOrDefault(queueId);

    private void SetState(Guid queueId, QueueStorageState state)
        => _states[queueId] = state;

    private void RemoveState(Guid queueId)
        => _states.TryRemove(queueId, out _);
    
    private static string GetQueuePath(Guid queueId) 
        => $"{QueuesDirectory}/{queueId:N}";
    
    private static string GetMessagesPath(Guid queueId)
        => $"{GetQueuePath(queueId)}/{MessagesDirectory}";

    private static string GetQueueMetadataPath(Guid queueId) 
        => $"{GetQueuePath(queueId)}/{QueueMetadataFile}";
    
    private static string GetSegmentPath(Guid queueId, int segment)
        => $"{GetMessagesPath(queueId)}/{SegmentPrefix}{segment:D6}{SegmentExtension}";
}